using System.Security.Claims;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Staj_Proje_1.Models;
using Staj_Proje_1.Services;
using OB = Staj_Proje_1.Models.OpenBanking;

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
        public async Task<ActionResult<IEnumerable<OB.BankDto>>> GetLinkedBanks(CancellationToken ct)
        {
            var userId = await GetUserIdAsync();
            var list = await _svc.GetLinkedBanksAsync(userId, ct);
            return Ok(list);
        }

        [HttpPost("link")]
        public async Task<ActionResult<OB.BankDto>> Link([FromBody] OB.LinkBankRequest req, CancellationToken ct)
        {
            var userId = await GetUserIdAsync();
            var dto = await _svc.LinkBankAsync(userId, req, ct);
            return Ok(dto);
        }

        [HttpGet("accounts")]
        public async Task<ActionResult<IEnumerable<OB.AccountDto>>> Accounts([FromQuery] OB.BankCode bank, CancellationToken ct)
        {
            var userId = await GetUserIdAsync();
            var list = await _svc.GetAccountsAsync(userId, bank, ct);
            return Ok(list);
        }

        [HttpGet("recent-transactions")]
        public async Task<ActionResult<IEnumerable<OB.TransactionDto>>> Recent([FromQuery] OB.BankCode bank, [FromQuery] int take = 5, CancellationToken ct = default)
        {
            var userId = await GetUserIdAsync();
            var list = await _svc.GetRecentTransactionsAsync(userId, bank, take, ct);
            return Ok(list);
        }

        // =========================
        // NEW: MyBank Hesap Oluştur
        // =========================

        public record CreateMyBankAccountRequest(string? Name, string? Currency);

        private static string MaskIban(string? iban)
        {
            if (string.IsNullOrWhiteSpace(iban)) return "TR** **** **** **** **** **";
            var clean = System.Text.RegularExpressions.Regex.Replace(iban, @"\s+", "");
            if (clean.Length <= 10) return clean;

            var start = clean[..6];
            var end   = clean[^4..];
            var middle = new string('*', clean.Length - 10);
            var masked = start + middle + end;
            return System.Text.RegularExpressions.Regex.Replace(masked, ".{4}", "$0 ").Trim();
        }

        // Frontend: POST /api/open-banking/mybank/accounts
        [HttpPost("mybank/accounts")]
        [Consumes("application/json")]
        [ProducesResponseType(typeof(OB.AccountDto), StatusCodes.Status201Created)]
        public async Task<ActionResult<OB.AccountDto>> CreateMyBankAccount([FromBody] CreateMyBankAccountRequest req, CancellationToken ct)
        {
            var userId = await GetUserIdAsync();

            var created = await _svc.CreateMyBankAccountAsync(
                userId,
                req?.Name,
                req?.Currency,
                ct
            );

            // Positional ctor (id, bankCode, name, iban, balance, currency)
            var dto = new OB.AccountDto(
                created.Id.ToString(),
                OB.BankCode.mybank,
                string.IsNullOrWhiteSpace(created.AccountName)
                    ? $"{(string.IsNullOrWhiteSpace(created.Currency) ? "TRY" : created.Currency)} Hesabı"
                    : created.AccountName,
                created.Iban,            // maskesiz
                created.Balance,         // 0
                string.IsNullOrWhiteSpace(created.Currency) ? "TRY" : created.Currency
            );

            return CreatedAtAction(
                nameof(Accounts),
                new { bank = OB.BankCode.mybank },
                dto
            );
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
