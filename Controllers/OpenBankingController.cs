using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Staj_Proje_1.Models.OpenBanking;
using Staj_Proje_1.Services;
using Microsoft.AspNetCore.Identity;
using Staj_Proje_1.Models;

namespace Staj_Proje_1.Controllers
{
    [ApiController]
    [Route("api/open-banking")] // <- standart, Angular ile aynı
    public class OpenBankingController : ControllerBase
    {
        private readonly IOpenBankingService _svc;
        private readonly UserManager<ApplicationUser> _userManager;

        public OpenBankingController(IOpenBankingService svc, UserManager<ApplicationUser> userManager)
        {
            _svc = svc;
            _userManager = userManager;
        }

        // Yardımcı: Swagger’dan test ederken userId elde et
        private async Task<string> GetUserIdAsync()
        {
            var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(uid)) return uid;

            // Swagger’dan yetkisiz test için fallback (test14@gmail.com'u kendi mailinle değiştir)
            var test = await _userManager.FindByEmailAsync("test14@gmail.com");
            if (test is null) throw new InvalidOperationException("Kullanıcı bulunamadı. Önce register/login yap.");
            return test.Id;
        }

        [HttpGet("linked-banks")]
        public async Task<ActionResult<IEnumerable<BankDto>>> GetLinkedBanks(CancellationToken ct)
        {
            var userId = await GetUserIdAsync();
            var list = await _svc.GetLinkedBanksAsync(userId, ct);
            return Ok(list);
        }

        [HttpPost("link")]
        public async Task<ActionResult<BankDto>> Link([FromBody] LinkBankRequest req, CancellationToken ct)
        {
            var userId = await GetUserIdAsync();
            var dto = await _svc.LinkBankAsync(userId, req, ct);
            return Ok(dto);
        }

        [HttpGet("accounts")]
        public async Task<ActionResult<IEnumerable<AccountDto>>> Accounts([FromQuery] BankCode bank, CancellationToken ct)
        {
            var userId = await GetUserIdAsync();
            var list = await _svc.GetAccountsAsync(userId, bank, ct);
            return Ok(list);
        }

        [HttpGet("recent-transactions")]
        public async Task<ActionResult<IEnumerable<Models.TransactionDto>>> Recent([FromQuery] BankCode bank, [FromQuery] int take = 5, CancellationToken ct = default)
        {
            var userId = await GetUserIdAsync();
            var list = await _svc.GetRecentTransactionsAsync(userId, bank, take, ct);
            return Ok(list);
        }
    }
}
