// Controllers/MyBankTransactionsController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Staj_Proje_1.Data;
using Staj_Proje_1.Models.Dtos;

namespace Staj_Proje_1.Controllers
{
    [ApiController]
    [Route("api/mybank/transactions")]
    public class MyBankTransactionsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public MyBankTransactionsController(ApplicationDbContext db) => _db = db;

        /// <summary>
        /// Giriş yapan kullanıcının tüm MyBank hesaplarındaki işlemleri tek listede döner.
        /// Örn: /api/mybank/transactions/recent?take=10&skip=0&accountId=123&currency=TRY&direction=IN
        /// </summary>
        [HttpGet("recent")]
        public async Task<IActionResult> GetRecent(
            [FromQuery] int take = 10,
            [FromQuery] int skip = 0,
            [FromQuery] int? accountId = null,
            [FromQuery] string? currency = null,
            [FromQuery] string? direction = null,
            CancellationToken ct = default)
        {
            take = Math.Clamp(take, 1, 100);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            // Kullanıcının hesapları + işlemler
            var q =
                from t in _db.MyBankTransactions
                join a in _db.MyBankAccounts on t.MyBankAccountId equals a.Id
                where a.OwnerUserId == userId
                select new { t, a };

            if (accountId.HasValue)
                q = q.Where(x => x.a.Id == accountId.Value);

            if (!string.IsNullOrWhiteSpace(currency))
                q = q.Where(x => x.t.Currency == currency);

            if (!string.IsNullOrWhiteSpace(direction))
                q = q.Where(x => x.t.Direction == direction);

            var ordered = q.OrderByDescending(x => x.t.TransactionDate);

            var total = await ordered.CountAsync(ct);
            var items = await ordered
                .Skip(skip)
                .Take(take)
                .Select(x => new RecentTxDto(
                    x.t.Id,                              // long Id
                    x.a.Id,                              // int AccountId
                    x.a.AccountName ?? x.a.AccountNumber,// string AccountName
                    x.t.TransactionDate,                 // DateTime TransactionDate
                    x.t.Description,                     // string? Description
                    x.t.Direction!,                      // string Direction ("IN"/"OUT")
                    x.t.Amount,                          // decimal Amount
                    x.t.BalanceAfter,                    // decimal? BalanceAfter
                    x.t.Currency                         // string Currency
                ))
                .ToListAsync(ct);

            return Ok(new { total, items });
        }

        /// <summary>
        /// Belirli bir hesaba ait işlemler (sayfalı).
        /// Örn: /api/mybank/transactions/by-account/123?page=1&pageSize=10
        /// </summary>
        [HttpGet("by-account/{accountId:int}")]
        public async Task<IActionResult> ListByAccount(
            int accountId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            // Hesap gerçekten kullanıcıya mı ait?
            var owns = await _db.MyBankAccounts.AnyAsync(a => a.Id == accountId && a.OwnerUserId == userId, ct);
            if (!owns) return NotFound(new { message = "Hesap bulunamadı." });

            var baseQuery =
                from t in _db.MyBankTransactions
                where t.MyBankAccountId == accountId
                orderby t.TransactionDate descending
                select t;

            var total = await baseQuery.CountAsync(ct);
            var data = await baseQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Join(_db.MyBankAccounts,
                      t => t.MyBankAccountId,
                      a => a.Id,
                      (t, a) => new RecentTxDto(
                          t.Id,                           // long Id
                          a.Id,                           // int AccountId
                          a.AccountName ?? a.AccountNumber,// string AccountName
                          t.TransactionDate,              // DateTime TransactionDate
                          t.Description,                  // string? Description
                          t.Direction!,                   // string Direction
                          t.Amount,                       // decimal Amount
                          t.BalanceAfter,                 // decimal? BalanceAfter
                          t.Currency                      // string Currency
                      ))
                .ToListAsync(ct);

            return Ok(new { total, page, pageSize, items = data });
        }
    }
}
