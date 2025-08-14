using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Staj_Proje_1.Models;
using Staj_Proje_1.Models.Dtos;

namespace Staj_Proje_1.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // -> /api/auth
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<AuthController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        // POST /api/auth/register
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var exists = await _userManager.FindByEmailAsync(dto.Email);
            if (exists != null)
                return Conflict(new { message = "Bu e-posta ile kullanıcı zaten var." });

            var user = new ApplicationUser
            {
                UserName = dto.Email,  // Identity için zorunlu
                Email = dto.Email,
                PhoneNumber = dto.Phone,
                // İsterseniz ApplicationUser'da varsa:
                // KullaniciAdi = dto.FullName,
                // Bakiye = 0m,
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description);
                return BadRequest(new { message = "Kayıt başarısız.", errors });
            }

            return Ok(new { message = "Kayıt başarılı." });
        }

        // POST /api/auth/login
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user is null)
                return Unauthorized(new { message = "E-posta veya şifre hatalı." });

            var result = await _signInManager.PasswordSignInAsync(
                user, dto.Password, isPersistent: true, lockoutOnFailure: false);

            if (!result.Succeeded)
                return Unauthorized(new { message = "E-posta veya şifre hatalı." });

            // Cookie ile oturum açıldı. İsterseniz symbolik bir token da dönebilirsiniz.
            return Ok(new { message = "Giriş başarılı.", token = (string?)null });
        }

        // POST /api/auth/logout
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return Ok(new { message = "Çıkış yapıldı." });
        }
    }
}
