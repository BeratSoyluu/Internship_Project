using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Staj_Proje_1.Models;
using Staj_Proje_1.Services;
using Staj_Proje_1.Models.Dtos;
using Staj_Proje_1.Data; // <-- ApplicationDbContext

namespace Staj_Proje_1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MyBankController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly IBankService _myBankService;
        private readonly ILogger<MyBankController> _logger;
        private readonly ApplicationDbContext _db; // <-- eklendi

        public MyBankController(
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration,
            IBankService myBankService,
            ILogger<MyBankController> logger,
            ApplicationDbContext db) // <-- eklendi
        {
            _userManager = userManager;
            _configuration = configuration;
            _myBankService = myBankService;
            _logger = logger;
            _db = db; // <-- eklendi
        }

        // POST: /api/mybank/register
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterDto model)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var exists = await _userManager.FindByEmailAsync(model.Email);
            if (exists != null)
                return Conflict(new { message = "Bu e-posta zaten kayıtlı." });

            // Görünen ad: FirstName + LastName, yoksa email local-part
            var fullName = $"{model.FirstName} {model.LastName}".Trim();
            var displayName = !string.IsNullOrWhiteSpace(fullName) ? fullName : model.Email.Split('@')[0];

            var initialBalance = model.Bakiye ?? 0m;

            var user = new ApplicationUser
            {
                UserName    = model.Email,
                Email       = model.Email,
                PhoneNumber = model.Phone,
                KullaniciAdi = displayName,
                Bakiye       = initialBalance
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToArray();
                _logger.LogWarning("MyBank register failed for {Email}: {Errors}", model.Email, string.Join("; ", errors));
                return BadRequest(new { message = "Kayıt başarısız.", errors });
            }

            // ✅ Varsayılan hesap + IBAN oluştur
            var account = new MyBankAccount
            {
                OwnerUserId   = user.Id,                         // FK (modelinde olmalı)
                Iban          = await GenerateUniqueIbanAsync(), // benzersiz IBAN
                AccountNumber = GenerateAccountNumber(),
                AccountName   = $"Vadesiz - {displayName}",
                Currency      = "TRY",
                PhoneNumber   = model.Phone ?? "",
                CreatedAt     = DateTime.UtcNow
            };

            _db.MyBankAccounts.Add(account);
            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = "Kayıt başarılı.",
                account = new { account.Iban, account.AccountNumber, account.Currency }
            });
        }

        // POST: /api/mybank/login
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return Unauthorized(new { message = "E-posta veya şifre hatalı." });

            var ok = await _userManager.CheckPasswordAsync(user, model.Password);
            if (!ok)
                return Unauthorized(new { message = "E-posta veya şifre hatalı." });

            var token = GenerateJwtToken(user);
            return Ok(new
            {
                message = "Giriş başarılı.",
                token,
                user = new
                {
                    id = user.Id,
                    email = user.Email,
                    kullaniciAdi = user.KullaniciAdi,
                    bakiye = user.Bakiye
                }
            });
        }

        // GET: /api/mybank/account-details
        [HttpGet("account-details")]
        [Authorize]
        public async Task<IActionResult> GetAccountDetailsForLoggedUser()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Kullanıcı kimliği okunamadı." });

            try
            {
                var bankAccessToken = await _myBankService.GetTokenAsync();
                var accountList = await _myBankService.GetAccountListAsync(bankAccessToken);
                return Ok(accountList);
            }
            catch (ApplicationException ex)
            {
                _logger.LogError(ex, "Bank API error");
                return StatusCode(502, new { error = "Bank API error", detail = ex.Message });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error while calling Bank API");
                return StatusCode(502, new { error = "Http error", detail = ex.Message });
            }
        }

        // ----------------- Helpers -----------------
        private string GenerateJwtToken(ApplicationUser user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(JwtRegisteredClaimNames.Sub,  user.Email ?? user.UserName ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Email,user.Email ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName ?? string.Empty),
                new Claim("kullaniciAdi", user.KullaniciAdi ?? string.Empty),
                new Claim("bakiye", (user.Bakiye ?? 0m).ToString("0.##")),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var keyStr = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key eksik.");
            var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyStr));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var issuer   = _configuration["Jwt:Issuer"];
            var audience = _configuration["Jwt:Audience"];

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // DEMO: benzersiz IBAN (gerçek MOD-97 değil)
        private async Task<string> GenerateUniqueIbanAsync()
        {
            // Benzersiz olana kadar dener
            while (true)
            {
                var iban = GenerateTestIban();
                var exists = await Task.FromResult(_db.MyBankAccounts.Any(a => a.Iban == iban));
                if (!exists) return iban;
            }
        }

        private static string GenerateTestIban()
        {
            // TR + "00" + banka kodu + 16 haneli hesap (DEMO)
            var bankCode = "00061";
            var rnd = Random.Shared.NextInt64(1_0000_0000_0000_0000, 9_9999_9999_9999_9999);
            return $"TR00{bankCode}{rnd}";
        }

        private static string GenerateAccountNumber()
        {
            return Random.Shared.NextInt64(100000000000, 999999999999).ToString();
        }
    }
}
