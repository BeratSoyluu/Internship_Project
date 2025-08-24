using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staj_Proje_1.Data;
using Staj_Proje_1.Models;
using Staj_Proje_1.Models.Dtos;
using Staj_Proje_1.Services;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;

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
            if (!Regex.IsMatch(iban, @"^[A-Z]{2}[0-9A-Z]+$"))
                return false;

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

            // Harf ve rakam dışını at, boşlukları kaldır
            var sb = new StringBuilder(s.Length);
            foreach (var ch in s)
            {
                if (char.IsLetterOrDigit(ch)) sb.Append(ch);
            }
            return sb.ToString();
        }

        private static bool NamesMatch(string provided, string expected)
        {
            var p = FoldForCompare(provided);
            var e = FoldForCompare(expected);
            if (p.Length == 0 || e.Length == 0) return false;
            if (p == e) return true;
            // Esnek: biri diğerini içeriyorsa da kabul
            return p.Contains(e) || e.Contains(p);
        }

        private static string? ComposeFullName(ApplicationUser user)
        {
            // Reflection ile FullName / Ad / Soyad var mı bak
            var t = user.GetType();
            string? fromFull = t.GetProperty("FullName")?.GetValue(user)?.ToString()?.Trim();
            if (!string.IsNullOrWhiteSpace(fromFull)) return fromFull;

            var ad = t.GetProperty("Ad")?.GetValue(user)?.ToString()?.Trim();
            var soyad = t.GetProperty("Soyad")?.GetValue(user)?.ToString()?.Trim();
            var composed = $"{ad} {soyad}".Trim();
            if (!string.IsNullOrWhiteSpace(composed) && composed != "")
                return composed;

            return user.UserName;
        }

        // Aktif kullanıcının varsayılan "gönderen" hesabını seç
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

            // Fallback: sistemdeki ilk hesap
            return await _db.MyBankAccounts
                .OrderBy(a => a.CreatedAt)
                .FirstOrDefaultAsync(ct);
        }

        // ================== CREATE (GERÇEK GÖNDERİM + BAKİYE) ==================
        [HttpPost]
        [ProducesResponseType(typeof(TransferDto), StatusCodes.Status201Created)]
        public async Task<IActionResult> Create([FromBody] TransferCreateDto dto, CancellationToken ct)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var from = await ResolveFromAccountAsync(ct);
            if (from is null)
                return BadRequest(new { message = "Gönderen hesap bulunamadı. Sisteminizde en az bir MyBank hesabı olmalı." });

            var iban = NormalizeIban(dto.ToIban);
            if (!IsValidIban(iban))
                return BadRequest(new { message = "Geçersiz IBAN." });

            // Bakiye kontrolü
            ApplicationUser? sender = null;
            if (!string.IsNullOrEmpty(from.OwnerUserId))
            {
                sender = await _db.Users.FirstOrDefaultAsync(u => u.Id == from.OwnerUserId, ct);
                if (sender != null && sender.Bakiye < dto.Amount)
                    return BadRequest(new { message = "Yetersiz bakiye." });
            }

            // IBAN bizim sistemdeki bir MyBank hesabına ait ise isim doğrula
            string? expectedReceiverName = null;
            var recvAccByIban = await _db.MyBankAccounts.FirstOrDefaultAsync(a => a.Iban == iban, ct);
            if (recvAccByIban?.OwnerUserId != null)
            {
                var recvUser = await _db.Users.FirstOrDefaultAsync(u => u.Id == recvAccByIban.OwnerUserId, ct);
                if (recvUser != null)
                {
                    expectedReceiverName = ComposeFullName(recvUser);
                    if (!string.IsNullOrWhiteSpace(expectedReceiverName))
                    {
                        var providedName = dto.ToName ?? string.Empty;
                        if (!NamesMatch(providedName, expectedReceiverName))
                            return BadRequest(new { message = "Alıcı adı IBAN ile uyuşmuyor." });
                    }
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
                var toNameSafe = string.IsNullOrWhiteSpace(entity.ToName)
                    ? (expectedReceiverName ?? string.Empty)
                    : entity.ToName;

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

                    if (sender != null)
                    {
                        sender.Bakiye -= entity.Amount;
                        _db.Users.Update(sender);
                    }

                    // Alıcı sistemdeyse onun bakiyesini de arttır
                    if (recvAccByIban?.OwnerUserId != null)
                    {
                        var receiver = await _db.Users.FirstOrDefaultAsync(u => u.Id == recvAccByIban.OwnerUserId, ct);
                        if (receiver != null)
                        {
                            receiver.Bakiye += entity.Amount;
                            _db.Users.Update(receiver);
                        }
                    }
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

            // IBAN bizim sistemdeyse tekrar da isim doğrulaması yap
            string? expectedReceiverName = null;
            var recvAccByIban = await _db.MyBankAccounts.FirstOrDefaultAsync(a => a.Iban == t.ToIban, ct);
            if (recvAccByIban?.OwnerUserId != null)
            {
                var recvUser = await _db.Users.FirstOrDefaultAsync(u => u.Id == recvAccByIban.OwnerUserId, ct);
                if (recvUser != null)
                {
                    expectedReceiverName = ComposeFullName(recvUser);
                    if (!string.IsNullOrWhiteSpace(expectedReceiverName))
                    {
                        var providedName = t.ToName ?? string.Empty;
                        if (!NamesMatch(providedName, expectedReceiverName))
                            return BadRequest(new { message = "Alıcı adı IBAN ile uyuşmuyor." });
                    }
                }
            }

            using var tx = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                // Bakiye kontrolü
                ApplicationUser? sender = null;
                if (!string.IsNullOrEmpty(t.FromAccount?.OwnerUserId))
                {
                    sender = await _db.Users.FirstOrDefaultAsync(u => u.Id == t.FromAccount.OwnerUserId, ct);
                    if (sender != null && sender.Bakiye < t.Amount)
                        return BadRequest(new { message = "Yetersiz bakiye." });
                }

                var token = await _bank.GetTokenAsync(ct);

                var toNameSafe = string.IsNullOrWhiteSpace(t.ToName)
                    ? (expectedReceiverName ?? string.Empty)
                    : t.ToName;

                var res = await _bank.SendTransferAsync(
                    token,
                    t.FromAccount.AccountNumber,
                    t.ToIban,
                    toNameSafe,
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

                    if (sender != null)
                    {
                        sender.Bakiye -= t.Amount;
                        _db.Users.Update(sender);
                    }

                    // Alıcı sistemdeyse onun bakiyesini arttır
                    if (recvAccByIban?.OwnerUserId != null)
                    {
                        var receiver = await _db.Users.FirstOrDefaultAsync(u => u.Id == recvAccByIban.OwnerUserId, ct);
                        if (receiver != null)
                        {
                            receiver.Bakiye += t.Amount;
                            _db.Users.Update(receiver);
                        }
                    }
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
