using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staj_Proje_1.Data;
using Staj_Proje_1.Models;
using Staj_Proje_1.Models.Dtos;
using Staj_Proje_1.Services;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

namespace Staj_Proje_1.Controllers
{
    [ApiController]
    [Route("api/mybank/transfers")]
    public class MyBankTransfersController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IBankService _bank;

        public MyBankTransfersController(ApplicationDbContext db, IBankService bank)
        {
            _db = db;
            _bank = bank;
        }

        // ----------------- IBAN yardımcıları -----------------
        private static string NormalizeIban(string iban) =>
            (iban ?? "").Replace(" ", "").ToUpperInvariant();

        private static bool IsValidIban(string iban)
        {
            iban = NormalizeIban(iban);
            if (iban.Length < 15 || iban.Length > 34) return false;
            if (!Regex.IsMatch(iban, @"^[A-Z]{2}[0-9A-Z]+$")) return false;

            // IBAN checksum (mod 97)
            string rearranged = iban[4..] + iban[..4];
            var sb = new StringBuilder(rearranged.Length * 2);
            foreach (char c in rearranged)
                sb.Append(char.IsLetter(c) ? (c - 'A' + 10).ToString() : c);

            string s = sb.ToString();
            int chunk = 0;
            foreach (char ch in s)
                chunk = (chunk * 10 + (ch - '0')) % 97;

            return chunk == 1;
        }

        // ----------------- İsim karşılaştırma yardımcıları -----------------
        private static string FoldForCompare(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            string s = input.Trim().ToUpperInvariant();

            // TR karakter düzeltmeleri
            s = s
                .Replace('İ', 'I')
                .Replace('I', 'I')
                .Replace('ı', 'I')
                .Replace('Ş', 'S').Replace('ş', 'S')
                .Replace('Ğ', 'G').Replace('ğ', 'G')
                .Replace('Ü', 'U').Replace('ü', 'U')
                .Replace('Ö', 'O').Replace('ö', 'O')
                .Replace('Ç', 'C').Replace('ç', 'C');

            // Harf ve rakam dışını at
            var sb = new StringBuilder(s.Length);
            foreach (var ch in s)
            {
                if (char.IsLetterOrDigit(ch)) sb.Append(ch);
            }
            return sb.ToString();
        }

        // "Vadesiz - Ahmet Yılmaz", "Hesap - Ayşe" vb. ön ekleri kaldır
        private static string StripAccountNamePrefixes(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            var s = input.Trim();

            // En sık görülen ayraç sonrası kısmı al (solda "Vadesiz", "Hesap" vs. varsa)
            var parts = s.Split(new[] { '-', '–', ':' }, 2, StringSplitOptions.TrimEntries);
            if (parts.Length == 2)
            {
                var left = FoldForCompare(parts[0]);
                string[] known = { "VADESIZ", "HESAP", "MEVDUAT", "KART", "MYBANK", "VAKIFBANK", "VAKIF" };
                if (known.Any(k => left.StartsWith(k))) return parts[1];
            }
            return s;
        }

        private static bool NamesMatch(string provided, string expected)
        {
            var p = FoldForCompare(provided);
            var e = FoldForCompare(expected);
            if (p.Length == 0 || e.Length == 0) return false;
            if (p == e) return true;
            return p.Contains(e) || e.Contains(p);
        }

        private static string? ComposeFullName(ApplicationUser user)
        {
            var t = user.GetType();
            string? fromFull = t.GetProperty("FullName")?.GetValue(user)?.ToString()?.Trim();
            if (!string.IsNullOrWhiteSpace(fromFull)) return fromFull;

            var ad = t.GetProperty("Ad")?.GetValue(user)?.ToString()?.Trim();
            var soyad = t.GetProperty("Soyad")?.GetValue(user)?.ToString()?.Trim();
            var composed = $"{ad} {soyad}".Trim();
            if (!string.IsNullOrWhiteSpace(composed)) return composed;

            return user.UserName;
        }

        private async Task<MyBankAccount?> ResolveFromAccountAsync(CancellationToken ct)
        {
            var userId = User?.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!string.IsNullOrEmpty(userId))
            {
                var mine = await _db.MyBankAccounts
                    .Where(a => a.OwnerUserId == userId)
                    .OrderBy(a => a.CreatedAt)
                    .ToListAsync(ct);

                if (mine.Count > 0) return mine[0];
            }

            // Fallback
            return await _db.MyBankAccounts
                .OrderBy(a => a.CreatedAt)
                .FirstOrDefaultAsync(ct);
        }

        // --- Yardımcılar: bakiye etki ve kullanıcı toplamı yeniden hesap ---
        private async Task RecalcUserBalanceAsync(string userId, CancellationToken ct)
        {
            var sum = await _db.MyBankAccounts
                .Where(a => a.OwnerUserId == userId)
                .SumAsync(a => a.Balance, ct);

            var u = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId, ct);
            if (u != null)
            {
                u.Bakiye = sum;
                _db.Users.Update(u);
            }
        }

        private async Task ApplyBalanceEffectsAsync(MyBankAccount from, string toIban, decimal amount, CancellationToken ct)
        {
            // Gönderen hesabı düş
            from.Balance -= amount;
            _db.MyBankAccounts.Update(from);

            // Alıcı bizim sistemdeyse ekle
            var recvAcc = await _db.MyBankAccounts.FirstOrDefaultAsync(a => a.Iban == toIban, ct);
            if (recvAcc != null)
            {
                recvAcc.Balance += amount;
                _db.MyBankAccounts.Update(recvAcc);

                if (!string.IsNullOrEmpty(recvAcc.OwnerUserId))
                    await RecalcUserBalanceAsync(recvAcc.OwnerUserId, ct);
            }

            if (!string.IsNullOrEmpty(from.OwnerUserId))
                await RecalcUserBalanceAsync(from.OwnerUserId, ct);
        }

        // ================== CREATE ==================
        [HttpPost]
        [ProducesResponseType(typeof(TransferDto), StatusCodes.Status201Created)]
        public async Task<IActionResult> Create([FromBody] TransferCreateDto dto, CancellationToken ct)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (dto.Amount <= 0) return BadRequest(new { message = "Tutar 0'dan büyük olmalı." });

            var from = await ResolveFromAccountAsync(ct);
            if (from is null)
                return BadRequest(new { message = "Gönderen hesap bulunamadı. Sisteminizde en az bir MyBank hesabı olmalı." });

            var iban = NormalizeIban(dto.ToIban);
            if (!IsValidIban(iban))
                return BadRequest(new { message = "Geçersiz IBAN." });

            // HESAP bakiyesi
            if (from.Balance < dto.Amount)
                return BadRequest(new { message = "Yetersiz bakiye." });

            // IBAN bizim sistemdeyse alıcı adı kontrolü (boş bırakılırsa kontrol yok)
            if (!string.IsNullOrWhiteSpace(dto.ToName))
            {
                var recvAccByIban = await _db.MyBankAccounts.FirstOrDefaultAsync(a => a.Iban == iban, ct);
                if (recvAccByIban?.OwnerUserId != null)
                {
                    var recvUser = await _db.Users.FirstOrDefaultAsync(u => u.Id == recvAccByIban.OwnerUserId, ct);

                    var expectedFromUser    = recvUser != null ? ComposeFullName(recvUser) : null;
                    var expectedFromAccount = StripAccountNamePrefixes(recvAccByIban.AccountName);

                    // İki kaynaktan da tutturamazsak reddet
                    var provided = dto.ToName ?? string.Empty;
                    var ok =
                        (!string.IsNullOrWhiteSpace(expectedFromUser)    && NamesMatch(provided, expectedFromUser)) ||
                        (!string.IsNullOrWhiteSpace(expectedFromAccount) && NamesMatch(provided, expectedFromAccount));

                    if (!ok)
                        return BadRequest(new { message = "Alıcı adı IBAN ile uyuşmuyor." });
                }
            }

            var entity = new MyBankTransfer
            {
                FromAccountId = from.Id,
                ToIban        = iban,
                ToName        = (dto.ToName ?? string.Empty).Trim(),
                Amount        = dto.Amount,
                Currency      = "TRY",
                Description   = null,
                Status        = TransferStatus.Pending,
                RequestedAt   = DateTime.UtcNow
            };
            _db.MyBankTransfers.Add(entity);
            await _db.SaveChangesAsync(ct);

            using var tx = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                var token = await _bank.GetTokenAsync(ct);
                var toNameSafe = entity.ToName;

                var res = await _bank.SendTransferAsync(
                    token,
                    from.AccountNumber,
                    iban,
                    toNameSafe,
                    dto.Amount,
                    "TRY",
                    null,
                    ct);

                if (res.Success)
                {
                    entity.Status        = TransferStatus.Completed;
                    entity.BankReference = string.IsNullOrWhiteSpace(res.Reference)
                        ? "RES-" + Guid.NewGuid().ToString("N")[..10].ToUpperInvariant()
                        : res.Reference;
                    entity.CompletedAt   = DateTime.UtcNow;

                    // bakiyeleri güncelle
                    await ApplyBalanceEffectsAsync(from, iban, entity.Amount, ct);
                }
                else
                {
                    entity.Status = TransferStatus.Failed;
                    Console.WriteLine($"[TRANSFER][FAIL] {res.StatusCode} - {res.StatusDescription}");
                }

                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync(ct);
                entity.Status = TransferStatus.Failed;
                await _db.SaveChangesAsync(ct);
                Console.WriteLine("[TRANSFER][EX] " + ex);
            }

            var resDto = new TransferDto
            {
                Id                = entity.Id,
                FromAccountId     = from.Id,
                FromAccountNumber = from.AccountNumber,
                ToIban            = entity.ToIban,
                ToName            = entity.ToName,
                Amount            = entity.Amount,
                Currency          = entity.Currency,
                Description       = entity.Description,
                Status            = entity.Status.ToString(),
                BankReference     = entity.BankReference,
                RequestedAt       = entity.RequestedAt,
                CompletedAt       = entity.CompletedAt
            };

            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, resDto);
        }

        // ================== GET BY ID ==================
        [HttpGet("{id:long}")]
        [ProducesResponseType(typeof(TransferDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(long id, CancellationToken ct)
        {
            var t = await _db.MyBankTransfers
                .Include(x => x.FromAccount)
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (t is null) return NotFound();

            var res = new TransferDto
            {
                Id                = t.Id,
                FromAccountId     = t.FromAccountId,
                FromAccountNumber = t.FromAccount?.AccountNumber ?? "",
                ToIban            = t.ToIban,
                ToName            = t.ToName,
                Amount            = t.Amount,
                Currency          = t.Currency,
                Description       = t.Description,
                Status            = t.Status.ToString(),
                BankReference     = t.BankReference,
                RequestedAt       = t.RequestedAt,
                CompletedAt       = t.CompletedAt
            };

            return Ok(res);
        }

        // ================== HESABA GÖRE LİSTE ==================
        [HttpGet("by-account/{accountId:int}")]
        public async Task<IActionResult> ListByAccount(
            int accountId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            var q = _db.MyBankTransfers.Where(t => t.FromAccountId == accountId);

            var total = await q.CountAsync(ct);
            var items = await q
                .OrderByDescending(t => t.RequestedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new TransferDto
                {
                    Id                = t.Id,
                    FromAccountId     = t.FromAccountId,
                    FromAccountNumber = t.FromAccount.AccountNumber,
                    ToIban            = t.ToIban,
                    ToName            = t.ToName,
                    Amount            = t.Amount,
                    Currency          = t.Currency,
                    Description       = t.Description,
                    Status            = t.Status.ToString(),
                    BankReference     = t.BankReference,
                    RequestedAt       = t.RequestedAt,
                    CompletedAt       = t.CompletedAt
                })
                .ToListAsync(ct);

            return Ok(new { total, page, pageSize, items });
        }

        // ================== TEKRAR GÖNDER ==================
        [HttpPost("{id:long}/send")]
        public async Task<IActionResult> Send(long id, CancellationToken ct)
        {
            var t = await _db.MyBankTransfers
                .Include(x => x.FromAccount)
                .FirstOrDefaultAsync(x => x.Id == id, ct);
            if (t is null) return NotFound();

            if (t.Status == TransferStatus.Completed)
                return BadRequest(new { message = "Transfer zaten gönderilmiş." });

            // IBAN bizim sistemdeyse tekrar isim doğrulaması (ToName boşsa yine esnek)
            if (!string.IsNullOrWhiteSpace(t.ToName))
            {
                var recvAccByIban = await _db.MyBankAccounts.FirstOrDefaultAsync(a => a.Iban == t.ToIban, ct);
                if (recvAccByIban?.OwnerUserId != null)
                {
                    var recvUser = await _db.Users.FirstOrDefaultAsync(u => u.Id == recvAccByIban.OwnerUserId, ct);

                    var expectedFromUser    = recvUser != null ? ComposeFullName(recvUser) : null;
                    var expectedFromAccount = StripAccountNamePrefixes(recvAccByIban.AccountName);

                    var provided = t.ToName ?? string.Empty;
                    var ok =
                        (!string.IsNullOrWhiteSpace(expectedFromUser)    && NamesMatch(provided, expectedFromUser)) ||
                        (!string.IsNullOrWhiteSpace(expectedFromAccount) && NamesMatch(provided, expectedFromAccount));

                    if (!ok)
                        return BadRequest(new { message = "Alıcı adı IBAN ile uyuşmuyor." });
                }
            }

            using var tx = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                if (t.FromAccount == null)
                    return BadRequest(new { message = "Kaynak hesap bulunamadı." });

                if (t.FromAccount.Balance < t.Amount)
                    return BadRequest(new { message = "Yetersiz bakiye." });

                var token = await _bank.GetTokenAsync(ct);

                var res = await _bank.SendTransferAsync(
                    token,
                    t.FromAccount.AccountNumber,
                    t.ToIban,
                    t.ToName ?? string.Empty,
                    t.Amount,
                    "TRY",
                    null,
                    ct);

                if (res.Success)
                {
                    t.Status        = TransferStatus.Completed;
                    t.BankReference = string.IsNullOrWhiteSpace(res.Reference)
                        ? t.BankReference ?? "RES-" + Guid.NewGuid().ToString("N")[..10].ToUpperInvariant()
                        : res.Reference;
                    t.CompletedAt   = DateTime.UtcNow;

                    await ApplyBalanceEffectsAsync(t.FromAccount, t.ToIban, t.Amount, ct);
                }
                else
                {
                    t.Status = TransferStatus.Failed;
                    Console.WriteLine($"[TRANSFER][FAIL] {res.StatusCode} - {res.StatusDescription}");
                }

                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync(ct);
                t.Status = TransferStatus.Failed;
                await _db.SaveChangesAsync(ct);
                Console.WriteLine("[TRANSFER][EX] " + ex);
            }

            return Ok(new { id = t.Id, status = t.Status.ToString(), reference = t.BankReference });
        }
    }
}
