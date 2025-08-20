using System.Security.Claims;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Staj_Proje_1.Models;
using Staj_Proje_1.Models.OpenBanking;
using Staj_Proje_1.Services;

namespace Staj_Proje_1.Controllers
{
    [ApiController]
    [Route("api/open-banking")] // Angular ile aynı
    public class OpenBankingController : ControllerBase
    {
        private readonly IOpenBankingService _svc;   // MyBank + ortak akış
        private readonly IBankService _bank;         // VakıfBank entegrasyonu
        private readonly UserManager<ApplicationUser> _userManager;

        public OpenBankingController(
            IOpenBankingService svc,
            IBankService bank,
            UserManager<ApplicationUser> userManager)
        {
            _svc = svc;
            _bank = bank;
            _userManager = userManager;
        }

        // Swagger için yardımcı (auth yoksa test kullanıcıya düş)
        private async Task<string> GetUserIdAsync()
        {
            var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(uid)) return uid;

            // Burayı kendi test mailinle güncelle
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

        // ---------------- VakıfBank: Account List (tablo için) ----------------

        // UI'daki tabloya bire bir uygun satır DTO'su
        public record VakifAccountRowDto(
            string Currency,                // Para Birimi
            DateTime? LastTransactionDate,  // Son İşlem Tarihi
            string Status,                  // Hesap Durumu
            string Iban,                    // IBAN
            decimal Balance,                // Bakiye
            string AccountType,             // Hesap Türü
            string AccountNumber            // Hesap Numarası
        );

        // Basit sağlık kontrolü
        [HttpGet("vakif/ping")]
        public IActionResult VakifPing() => Ok("vakif-ok");

        // GET /api/open-banking/vakif/account-list
        // NOT: [Authorize] ekli değil; dev/test’te kolay denemek için.
        [HttpGet("vakif/account-list")]
        public async Task<ActionResult<IEnumerable<VakifAccountRowDto>>> GetVakifAccountList(CancellationToken ct)
        {
            try
            {
                var token = await _bank.GetTokenAsync(ct);
                var res   = await _bank.GetAccountListAsync(token, ct);

                // Accounts koleksiyonunu yakala (res.Accounts ya da res.Data.Accounts)
                var accountsProp = res?.GetType().GetProperty("Accounts");
                IEnumerable<object> accounts;

                if (accountsProp != null)
                {
                    accounts = (IEnumerable<object>?)accountsProp.GetValue(res) ?? Enumerable.Empty<object>();
                }
                else
                {
                    var dataObj = res?.GetType().GetProperty("Data")?.GetValue(res);
                    accounts = (IEnumerable<object>?)dataObj?
                        .GetType().GetProperty("Accounts")?.GetValue(dataObj) ?? Enumerable.Empty<object>();
                }

                // ---- Güvenli property okuma helper’ları (case-insensitive) ----
                static object? GetProp(object obj, params string[] names)
                {
                    var t = obj.GetType();
                    foreach (var name in names)
                    {
                        var p = t.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                        if (p != null) return p.GetValue(obj);
                    }
                    return null;
                }

                static string GetString(object obj, params string[] names)
                    => Convert.ToString(GetProp(obj, names)) ?? string.Empty;

                static decimal GetDecimal(object obj, params string[] names)
                {
                    var v = GetProp(obj, names);
                    if (v == null) return 0m;
                    return v switch
                    {
                        decimal d => d,
                        double db => (decimal)db,
                        float f => (decimal)f,
                        int i => i,
                        long l => l,
                        _ => decimal.TryParse(Convert.ToString(v), out var parsed) ? parsed : 0m
                    };
                }

                static DateTime? GetDateTime(object obj, params string[] names)
                {
                    var v = GetProp(obj, names);
                    if (v == null) return null;
                    if (v is DateTime dt) return dt;
                    return DateTime.TryParse(Convert.ToString(v), out var parsed) ? parsed : null;
                }
                // ----------------------------------------------------------------

                var rows = accounts.Select(a => new VakifAccountRowDto(
                    Currency:            string.IsNullOrWhiteSpace(GetString(a, "Currency", "CurrencyCode", "currency"))
                                            ? "TL"
                                            : GetString(a, "Currency", "CurrencyCode", "currency"),
                    LastTransactionDate: GetDateTime(a, "LastTransactionDate", "LastTxnDate", "lastTransactionDate"),
                    Status:              string.IsNullOrWhiteSpace(GetString(a, "Status", "StatusText", "status"))
                                            ? "Aktif"
                                            : GetString(a, "Status", "StatusText", "status"),
                    Iban:                GetString(a, "Iban", "IBAN", "iban", "ibanNo"),
                    Balance:             GetDecimal(a, "Balance", "AvailableBalance", "CurrentBalance", "availableBalance"),
                    AccountType:         string.IsNullOrWhiteSpace(GetString(a, "AccountType", "AccountTypeName", "accountTypeName"))
                                            ? "Vadesiz Türk Parası Mevduat Hesabı"
                                            : GetString(a, "AccountType", "AccountTypeName", "accountTypeName"),
                    AccountNumber:       GetString(a, "AccountNumber", "AccountNo", "accountNumber")
                )).ToList();

                return Ok(rows);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "vakif_account_list_failed", detail = ex.Message });
            }
        }
    }
}
