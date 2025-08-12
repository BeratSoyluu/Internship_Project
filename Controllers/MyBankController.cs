using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Staj_Proje_1.Models;
using Staj_Proje_1.Services;

namespace Staj_Proje_1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MyBankController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly IBankService _myBankService;

        public MyBankController(
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration,
            IBankService myBankService)
        {
            _userManager = userManager;
            _configuration = configuration;
            _myBankService = myBankService;
        }

        // POST: /api/mybank/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto model)
        {
            var user = new ApplicationUser
            {
                UserName = model.KullaniciAdi ?? model.Email,
                Email = model.Email,
                KullaniciAdi = model.KullaniciAdi,
                Bakiye = model.Bakiye
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
                return Ok(new { message = "Kayıt başarılı" });

            return BadRequest(result.Errors);
        }

        // POST: /api/mybank/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null && await _userManager.CheckPasswordAsync(user, model.Password))
            {
                var token = GenerateJwtToken(user);
                return Ok(new { token });
            }
            return Unauthorized();
        }

        // GET: /api/mybank/account-details
        // Kullanıcının kendi JWT’si ile korunur; banka tarafı için ayrıyeten bankadan access token alınır.
        [HttpGet("account-details")]
        [Authorize]
        public async Task<IActionResult> GetAccountDetailsForLoggedUser()
        {
            // İstersen ileride userId ile yerel filtreleme yapabilirsin
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized("Kullanıcı kimliği okunamadı.");

            try
            {
                // 1) Banka access token’ını çek
                var bankAccessToken = await _myBankService.GetTokenAsync();

                // 2) Hesap listesini al
                var accountList = await _myBankService.GetAccountListAsync(bankAccessToken);

                // İstersen burada DTO’ya projekte edebilirsin; şimdilik ham yanıtı dönüyorum
                return Ok(accountList);
            }
            catch (ApplicationException ex)
            {
                return StatusCode(502, new { error = "Bank API error", detail = ex.Message });
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(502, new { error = "Http error", detail = ex.Message });
            }
        }

        private string GenerateJwtToken(ApplicationUser user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(JwtRegisteredClaimNames.Sub, user.Email ?? user.UserName ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
