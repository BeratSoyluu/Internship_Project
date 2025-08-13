using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staj_Proje_1.Data;
using Staj_Proje_1.Models;
using Staj_Proje_1.Models.Dtos;

namespace Staj_Proje_1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MyBankAccountsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public MyBankAccountsController(ApplicationDbContext db) => _db = db;

        [HttpPost]
        [ProducesResponseType(typeof(MyBankAccountDto), StatusCodes.Status201Created)]
        public async Task<IActionResult> Create([FromBody] MyBankAccountCreateDto dto, CancellationToken ct)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // IBAN normalize (boşluk-simge temizle)
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
                // OwnerUserId = User?.FindFirstValue(ClaimTypes.NameIdentifier) // varsa
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
    }
}
