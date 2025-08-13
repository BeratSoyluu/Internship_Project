using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staj_Proje_1.Data;
using Staj_Proje_1.Models;
using Staj_Proje_1.Models.Dtos;
using Staj_Proje_1.Services;

namespace Staj_Proje_1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MyBankAccountsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IBankService _bank;

        public MyBankAccountsController(ApplicationDbContext db, IBankService bank)
        {
            _db = db;
            _bank = bank;
        }

        // ===================== CRUD =====================

        [HttpPost]
        [ProducesResponseType(typeof(MyBankAccountDto), StatusCodes.Status201Created)]
        public async Task<IActionResult> Create([FromBody] MyBankAccountCreateDto dto, CancellationToken ct)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var iban = dto.Iban.Replace(" ", "").ToUpperInvariant();

            var exists = await _db.MyBankAccounts.AnyAsync(x => x.Iban == iban, ct);
            if (exists) return Conflict(new { message = "Bu IBAN ile kayıt zaten var." });

            var entity = new MyBankAccount
            {
                Iban = iban,
                AccountNumber = dto.AccountNumber.Trim(),
                PhoneNumber = dto.PhoneNumber.Trim(),
                AccountName = dto.AccountName,
                Currency = dto.Currency,
                BankName = "MyBank"
            };

            _db.MyBankAccounts.Add(entity);
            await _db.SaveChangesAsync(ct);

            var res = new MyBankAccountDto
            {
                Id = entity.Id,
                Iban = entity.Iban,
                AccountNumber = entity.AccountNumber,
                PhoneNumber = entity.PhoneNumber,
                AccountName = entity.AccountName,
                Currency = entity.Currency,
                BankName = entity.BankName,
                CreatedAt = entity.CreatedAt
            };

            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, res);
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(MyBankAccountDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
        {
            var e = await _db.MyBankAccounts.FindAsync(new object?[] { id }, ct);
            if (e is null) return NotFound();

            var res = new MyBankAccountDto
            {
                Id = e.Id,
                Iban = e.Iban,
                AccountNumber = e.AccountNumber,
                PhoneNumber = e.PhoneNumber,
                AccountName = e.AccountName,
                Currency = e.Currency,
                BankName = e.BankName,
                CreatedAt = e.CreatedAt
            };
            return Ok(res);
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<MyBankAccountDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> List(CancellationToken ct)
        {
            var list = await _db.MyBankAccounts
                .OrderByDescending(x => x.CreatedAt)
                .Select(e => new MyBankAccountDto
                {
                    Id = e.Id,
                    Iban = e.Iban,
                    AccountNumber = e.AccountNumber,
                    PhoneNumber = e.PhoneNumber,
                    AccountName = e.AccountName,
                    Currency = e.Currency,
                    BankName = e.BankName,
                    CreatedAt = e.CreatedAt
                })
                .ToListAsync(ct);

            return Ok(list);
        }

        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Update(int id, [FromBody] MyBankAccountUpdateDto dto, CancellationToken ct)
        {
            var e = await _db.MyBankAccounts.FindAsync(new object?[] { id }, ct);
            if (e is null) return NotFound();

            if (dto.PhoneNumber is not null) e.PhoneNumber = dto.PhoneNumber.Trim();
            if (dto.AccountName is not null) e.AccountName = dto.AccountName;

            e.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var e = await _db.MyBankAccounts.FindAsync(new object?[] { id }, ct);
            if (e is null) return NotFound();
            _db.MyBankAccounts.Remove(e);
            await _db.SaveChangesAsync(ct);
            return NoContent();
        }

        // ===================== HESAP DETAYLARI + SON İŞLEMLER =====================

        // Detay + son 10 işlem
        [HttpGet("{id:int}/details")]
        public async Task<IActionResult> GetDetails(int id, CancellationToken ct)
        {
            var acc = await _db.MyBankAccounts.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (acc is null) return NotFound(new { message = "Hesap bulunamadı." });

            var last10 = await _db.MyBankTransactions
                .Where(t => t.MyBankAccountId == id)
                .OrderByDescending(t => t.TransactionDate)
                .Take(10)
                .ToListAsync(ct);

            return Ok(new
            {
                account = new
                {
                    acc.Id,
                    acc.Iban,
                    acc.AccountNumber,
                    acc.PhoneNumber,
                    acc.AccountName,
                    acc.Currency,
                    createdAt = acc.CreatedAt
                },
                lastTransactions = last10
            });
        }

        // Son işlemler (sayfalı liste) ?page=1&pageSize=10
        [HttpGet("{id:int}/transactions")]
        public async Task<IActionResult> GetTransactions(
            int id,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            var total = await _db.MyBankTransactions.Where(t => t.MyBankAccountId == id).CountAsync(ct);

            var items = await _db.MyBankTransactions
                .Where(t => t.MyBankAccountId == id)
                .OrderByDescending(t => t.TransactionDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return Ok(new { total, page, pageSize, items });
        }

        // ===================== BANKADAN SENKRON =====================

        public class SyncTransactionsRequest
        {
            public string StartDate { get; set; } = ""; // dd-MM-yyyy
            public string EndDate   { get; set; } = ""; // dd-MM-yyyy
        }

        [HttpPost("{id:int}/sync-transactions")]
        public async Task<IActionResult> SyncTransactions(int id, [FromBody] SyncTransactionsRequest req, CancellationToken ct)
        {
            var acc = await _db.MyBankAccounts.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (acc is null) return NotFound(new { message = "Hesap bulunamadı." });

            // Bankadan çek
            var token = await _bank.GetTokenAsync(ct);
            var res = await _bank.GetAccountTransactionsAsync(
                token,
                acc.AccountNumber,
                req.StartDate, // dd-MM-yyyy
                req.EndDate,   // dd-MM-yyyy
                ct);

            // Not: Şu an DTO'nuzda sadece AccountTransactions var.
            var list = res.Data?.AccountTransactions ?? new List<TransactionDto>();

            int added = 0;
            foreach (var tx in list)
            {
                // Duplicate engeli: TransactionId varsa ona göre kontrol et
                if (!string.IsNullOrWhiteSpace(tx.TransactionId))
                {
                    bool exists = await _db.MyBankTransactions
                        .AnyAsync(t => t.MyBankAccountId == id && t.ExternalId == tx.TransactionId, ct);
                    if (exists) continue;
                }

                // Tarih parse
                DateTime date = DateTime.TryParse(tx.TransactionDate, out var d) ? d : DateTime.UtcNow;

                var entity = new MyBankTransaction
                {
                    MyBankAccountId = id,
                    TransactionDate = date,
                    Currency        = string.IsNullOrWhiteSpace(tx.Currency) ? "TRY" : tx.Currency,
                    Amount          = tx.Amount,
                    Direction       = tx.TransactionCode, // CR / DB varsa
                    Description     = tx.Description,
                    ExternalId      = tx.TransactionId,
                    BalanceAfter    = tx.Balance
                };

                _db.MyBankTransactions.Add(entity);
                added++;
            }

            await _db.SaveChangesAsync(ct);

            return Ok(new { synced = added, received = list.Count });
        }
    }
}
