// Controllers/MyBankTransactionsController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Linq;
using Staj_Proje_1.Data;
using Staj_Proje_1.Models.Dtos;
using Staj_Proje_1.Models;

namespace Staj_Proje_1.Controllers
{
    [ApiController]
    [Route("api/mybank/transactions")]
    public class MyBankTransactionsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public MyBankTransactionsController(ApplicationDbContext db) => _db = db;

        /// <summary>
        /// Giriş yapan kullanıcının tüm MyBank hesapları için "Son İşlemler".
        /// Kaynak: MyBankTransfers (Status=Completed) → OUT/IN satır projeksiyonu.
        /// </summary>
        [HttpGet("recent")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRecent(
            [FromQuery] int take = 10,
            [FromQuery] int skip = 0,
            [FromQuery] int? accountId = null,
            [FromQuery] string? currency = null,
            [FromQuery] string? direction = null,
            CancellationToken ct = default)
        {
            take = Math.Clamp(take, 1, 100);
            if (skip < 0) skip = 0;

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            string? cur = string.IsNullOrWhiteSpace(currency) ? null : currency!.Trim().ToUpperInvariant();
            string? dir = string.IsNullOrWhiteSpace(direction) ? null : direction!.Trim().ToUpperInvariant();

            // Kullanıcının MyBank hesapları
            var myAccs = await _db.MyBankAccounts
                .Where(a => a.OwnerUserId == userId)
                .Select(a => new { a.Id, a.Iban, a.AccountName, a.AccountNumber, a.Currency })
                .ToListAsync(ct);

            if (myAccs.Count == 0)
                return Ok(new { total = 0, items = Array.Empty<RecentTxDto>() });

            var myAccIds   = myAccs.Select(x => x.Id).ToList();
            var myIbansSet = myAccs.Select(x => x.Iban).ToHashSet(StringComparer.OrdinalIgnoreCase);

            // OUT: benim hesaplarımdan giden tamamlanmış transferler
            var outQ =
                from t in _db.MyBankTransfers
                join a in _db.MyBankAccounts on t.FromAccountId equals a.Id
                where myAccIds.Contains(a.Id)
                   && t.Status == TransferStatus.Completed
                   && t.CompletedAt != null
                select new
                {
                    Id          = t.Id,
                    AccountId   = a.Id,
                    AccountName = (a.AccountName ?? a.AccountNumber),
                    When        = t.CompletedAt!.Value,
                    Description = "Para transferi → " + (t.ToName ?? "") + " (" + t.ToIban + ")",
                    Direction   = "OUT",
                    Amount      = t.Amount,
                    Currency    = t.Currency
                };

            // IN: alıcı IBAN benim IBAN’larımdan biri ise
            var inQ =
                from t in _db.MyBankTransfers
                join a in _db.MyBankAccounts on t.ToIban equals a.Iban
                where myIbansSet.Contains(t.ToIban)
                   && t.Status == TransferStatus.Completed
                   && t.CompletedAt != null
                select new
                {
                    Id          = t.Id,
                    AccountId   = a.Id,
                    AccountName = (a.AccountName ?? a.AccountNumber),
                    When        = t.CompletedAt!.Value,
                    Description = "Para transferi ← " + (t.ToName ?? "") + " (" + t.ToIban + ")",
                    Direction   = "IN",
                    Amount      = t.Amount,
                    Currency    = t.Currency
                };

            // Filtreler
            if (accountId.HasValue) { outQ = outQ.Where(x => x.AccountId == accountId.Value); inQ = inQ.Where(x => x.AccountId == accountId.Value); }
            if (!string.IsNullOrEmpty(cur)) { outQ = outQ.Where(x => x.Currency == cur); inQ = inQ.Where(x => x.Currency == cur); }
            if (!string.IsNullOrEmpty(dir)) { outQ = outQ.Where(x => x.Direction == dir); inQ = inQ.Where(x => x.Direction == dir); }

            var unionQ = outQ.Concat(inQ);

            var total = await unionQ.CountAsync(ct);
            var page = await unionQ
                .OrderByDescending(x => x.When)
                .Skip(skip)
                .Take(take)
                .ToListAsync(ct);

            var items = page.Select(x => new RecentTxDto(
                x.Id,
                x.AccountId,
                x.AccountName,
                x.When,
                x.Description,
                x.Direction,
                x.Amount,
                null,        // BalanceAfter burada hesaplanmıyor
                x.Currency
            )).ToList();

            return Ok(new { total, items });
        }

        /// <summary>
        /// Belirli bir hesaba ait işlemler (sayfalı) — Transfers projeksiyonu.
        /// </summary>
        [HttpGet("by-account/{accountId:int}")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<IActionResult> ListByAccount(
            int accountId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            pageSize = Math.Clamp(pageSize, 1, 100);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var acc = await _db.MyBankAccounts
                .Where(a => a.Id == accountId && a.OwnerUserId == userId)
                .Select(a => new { a.Id, a.Iban, a.AccountName, a.AccountNumber })
                .FirstOrDefaultAsync(ct);

            if (acc is null) return NotFound(new { message = "Hesap bulunamadı." });

            var outQ =
                from t in _db.MyBankTransfers
                where t.FromAccountId == acc.Id
                   && t.Status == TransferStatus.Completed
                   && t.CompletedAt != null
                select new
                {
                    Id          = t.Id,
                    When        = t.CompletedAt!.Value,
                    Description = "Para transferi → " + (t.ToName ?? "") + " (" + t.ToIban + ")",
                    Direction   = "OUT",
                    Amount      = t.Amount,
                    Currency    = t.Currency
                };

            var inQ =
                from t in _db.MyBankTransfers
                where t.ToIban == acc.Iban
                   && t.Status == TransferStatus.Completed
                   && t.CompletedAt != null
                select new
                {
                    Id          = t.Id,
                    When        = t.CompletedAt!.Value,
                    Description = "Para transferi ← " + (t.ToName ?? "") + " (" + t.ToIban + ")",
                    Direction   = "IN",
                    Amount      = t.Amount,
                    Currency    = t.Currency
                };

            var unionQ = outQ.Concat(inQ);
            var total  = await unionQ.CountAsync(ct);

            var pageList = await unionQ
                .OrderByDescending(x => x.When)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            var items = pageList.Select(x => new RecentTxDto(
                x.Id,
                acc.Id,
                acc.AccountName ?? acc.AccountNumber,
                x.When,
                x.Description,
                x.Direction,
                x.Amount,
                null,
                x.Currency
            )).ToList();

            return Ok(new { total, page, pageSize, items });
        }
    }
}
