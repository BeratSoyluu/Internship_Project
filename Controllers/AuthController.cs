using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Staj_Proje_1.Models;
using Staj_Proje_1.Models.Dtos;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Staj_Proje_1.Data; // ✅ DbContext için
using System.Linq;       // Any, Select vs.

namespace Staj_Proje_1.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // /api/auth
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _cfg;
        private readonly ILogger<AuthController> _logger;
        private readonly ApplicationDbContext _db;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration cfg,
            ILogger<AuthController> logger,
            ApplicationDbContext db)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _cfg = cfg;
            _logger = logger;
            _db = db;
        }

        // POST /api/auth/register
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var exists = await _userManager.FindByEmailAsync(dto.Email);
            if (exists != null)
                return Conflict(new { message = "Bu e-posta ile kullanıcı zaten var." });

            // Görünen ad: (FirstName + LastName) varsa, yoksa email local-part
            var fullName = $"{dto.FirstName} {dto.LastName}".Trim();
            var displayName = !string.IsNullOrWhiteSpace(fullName)
                ? fullName
                : dto.Email.Split('@')[0];

            var initialBalance = dto.Bakiye ?? 0m;

            var user = new ApplicationUser
            {
                UserName    = dto.Email,
                Email       = dto.Email,
                PhoneNumber = dto.Phone,
                KullaniciAdi = displayName,
                Bakiye       = initialBalance
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToArray();
                _logger.LogWarning("Register failed for {Email}: {Errors}", dto.Email, string.Join("; ", errors));
                return BadRequest(new { message = "Kayıt başarısız.", errors });
            }

            // ✅ Kullanıcı oluştu → varsayılan hesap + MOD-97 kontrollü benzersiz TR IBAN üret
            const string bankCode = "00061"; // DEMO banka kodu (5 hane)
            var iban          = await GenerateUniqueTurkishIbanAsync(bankCode);
            var accountNumber = GenerateAccountNumber(12); // modelde max 20 ise 12 güvenli

            var myAccount = new MyBankAccount
            {
                OwnerUserId   = user.Id,
                BankName      = "MyBank",
                Iban          = iban,
                AccountNumber = accountNumber,
                AccountName   = $"Vadesiz - {displayName}",
                Currency      = "TRY",
                PhoneNumber   = dto.Phone ?? string.Empty,
                CreatedAt     = DateTime.UtcNow,
                UpdatedAt     = DateTime.UtcNow
            };

            _db.MyBankAccounts.Add(myAccount);
            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = "Kayıt başarılı.",
                account = new
                {
                    myAccount.Id,
                    myAccount.BankName,
                    myAccount.Iban,
                    myAccount.AccountNumber,
                    myAccount.Currency
                }
            });
        }

        // POST /api/auth/login
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user is null)
                return Unauthorized(new { message = "E-posta veya şifre hatalı." });

            var signIn = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: false);
            if (!signIn.Succeeded)
                return Unauthorized(new { message = "E-posta veya şifre hatalı." });

            var roles = await _userManager.GetRolesAsync(user);
            var (token, expiresUtc) = GenerateJwt(user, roles);

            return Ok(new
            {
                message = "Giriş başarılı.",
                token,
                expiresAtUtc = expiresUtc,
                user = new
                {
                    id = user.Id,
                    email = user.Email,
                    kullaniciAdi = user.KullaniciAdi,
                    bakiye = user.Bakiye,
                    roles
                }
            });
        }

        // POST /api/auth/logout
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return Ok(new { message = "Çıkış yapıldı." });
        }

        // GET /api/auth/me
        [HttpGet("me")]
        [Authorize]
        public IActionResult Me()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            var email  = User.FindFirstValue(ClaimTypes.Email)
                        ?? User.FindFirstValue(JwtRegisteredClaimNames.Email);
            var roles  = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToArray();

            return Ok(new { userId, email, roles });
        }

        // ----------------- JWT Helper -----------------
        private (string Token, DateTime ExpiresAtUtc) GenerateJwt(ApplicationUser user, IEnumerable<string> roles)
        {
            var jwtSection = _cfg.GetSection("Jwt");
            var keyStr   = jwtSection["Key"] ?? throw new InvalidOperationException("Jwt:Key yok");
            var issuer   = jwtSection["Issuer"];
            var audience = jwtSection["Audience"];
            var minsStr  = jwtSection["ExpireMinutes"];

            var key     = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyStr));
            var creds   = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var minutes = int.TryParse(minsStr, out var m) ? m : 60;
            var expires = DateTime.UtcNow.AddMinutes(minutes);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim("kullaniciAdi", user.KullaniciAdi ?? string.Empty),
                new Claim("bakiye", (user.Bakiye ?? 0m).ToString("0.##")),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: expires,
                signingCredentials: creds
            );

            return (new JwtSecurityTokenHandler().WriteToken(token), expires);
        }

        // ----------------- IBAN Helpers (GERÇEK MOD-97) -----------------

        // Benzersiz TR IBAN üret (MOD-97 kontrol rakamı ile)
        // Format: TR{kk}{bankCode(5)}{account(17)}  => toplam 26 karakter
        private async Task<string> GenerateUniqueTurkishIbanAsync(string bankCode)
        {
            if (string.IsNullOrWhiteSpace(bankCode) || bankCode.Length != 5 || !bankCode.All(char.IsDigit))
                throw new ArgumentException("bankCode 5 haneli numerik olmalıdır.", nameof(bankCode));

            for (int i = 0; i < 20; i++)
            {
                var account17 = GenerateDigits(17);
                var iban = BuildTurkishIban(bankCode, account17); // MOD-97 ile check digits

                var exists = _db.MyBankAccounts.Any(a => a.Iban == iban);
                if (!exists) return iban;

                await Task.Yield();
            }
            throw new InvalidOperationException("Benzersiz IBAN üretilemedi, tekrar deneyin.");
        }

        // TR IBAN üret: MOD-97 kontrol rakamlarını hesaplar
        private static string BuildTurkishIban(string bankCode, string account17)
        {
            var bban = bankCode + account17; // 22 hane (5 + 17)
            if (bban.Length != 22) throw new ArgumentException("BBAN uzunluğu 22 olmalıdır.");

            // 1) Geçici IBAN: TR00 + BBAN
            // 2) IBAN check: (BBAN + 'TR' + '00') -> sayıya çevir, mod 97
            // 3) Kontrol rakamı: 98 - mod97
            var rearranged = bban + "TR00";
            var numeric = ToIbanNumericString(rearranged); // A=10..Z=35
            var mod = Mod97(numeric);
            var check = 98 - mod;
            var kk = check.ToString("00");

            return $"TR{kk}{bban}";
        }

        // IBAN alfanumerik -> numerik (A=10..Z=35)
        private static string ToIbanNumericString(string input)
        {
            var sb = new StringBuilder(input.Length * 2);
            foreach (var ch in input)
            {
                if (char.IsLetter(ch))
                {
                    int val = char.ToUpperInvariant(ch) - 'A' + 10;
                    sb.Append(val);
                }
                else
                {
                    sb.Append(ch);
                }
            }
            return sb.ToString();
        }

        // Büyük sayı mod 97 (string bazlı)
        private static int Mod97(string numeric)
        {
            int chunkSize = 9;
            int remainder = 0;

            for (int i = 0; i < numeric.Length; i += chunkSize)
            {
                var len = Math.Min(chunkSize, numeric.Length - i);
                var part = numeric.Substring(i, len);
                var num = long.Parse(remainder.ToString() + part);
                remainder = (int)(num % 97);
            }
            return remainder;
        }

        // N haneli numerik hesap numarası üret
        private static string GenerateAccountNumber(int length)
        {
            if (length < 6 || length > 20) length = 12;
            return GenerateDigits(length);
        }

        // N haneli numerik dizge üret (ilk hane 0 değil)
        private static string GenerateDigits(int length)
        {
            var sb = new StringBuilder(length);
            var rng = Random.Shared;
            sb.Append((char)('1' + rng.Next(0, 9))); // ilk hane 1-9
            for (int i = 1; i < length; i++)
                sb.Append((char)('0' + rng.Next(0, 10)));
            return sb.ToString();
        }
    }
}
