using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Staj_Proje_1.Data;
using Staj_Proje_1.Models;                 // MyBankAccount (ve diğer domain modelleri)
using Staj_Proje_1.Models.OpenBanking;    // BankDto, AccountDto, BankCode, LinkBankRequest
using OB = Staj_Proje_1.Models.OpenBanking; // <- OpenBanking alias (TransactionDto için)

namespace Staj_Proje_1.Services
{
    public class OpenBankingService : IOpenBankingService
    {
        private readonly ApplicationDbContext _db;
        public OpenBankingService(ApplicationDbContext db) => _db = db;

        public async Task<IEnumerable<BankDto>> GetLinkedBanksAsync(string userId, CancellationToken ct = default)
        {
            var hasMyBank = await _db.MyBankAccounts
                .AsNoTracking()
                .AnyAsync(a => a.OwnerUserId == userId, ct);

            var myBankBalance = await _db.MyBankAccounts
                .AsNoTracking()
                .Where(a => a.OwnerUserId == userId)
                .SumAsync(a => (decimal?)a.Balance, ct) ?? 0m;

            var vakifBalance = 12345.67m; // dummy

            return new[]
            {
                new BankDto("vakif-1", "VakıfBank", BankCode.vakif,  vakifBalance, true),
                new BankDto("mybank-1","MyBank",    BankCode.mybank, myBankBalance, hasMyBank)
            };
        }

        public Task<BankDto> LinkBankAsync(string userId, LinkBankRequest req, CancellationToken ct = default)
        {
            var dto = req.BankCode switch
            {
                BankCode.vakif  => new BankDto("vakif-1","VakıfBank", BankCode.vakif,  0m, true),
                BankCode.mybank => new BankDto("mybank-1","MyBank",   BankCode.mybank, 0m, true),
                _               => throw new ArgumentOutOfRangeException(nameof(req.BankCode))
            };
            return Task.FromResult(dto);
        }

        public async Task<IEnumerable<AccountDto>> GetAccountsAsync(string userId, BankCode bank, CancellationToken ct = default)
        {
            if (bank == BankCode.vakif)
            {
                return new[]
                {
                    new AccountDto("acc-1", BankCode.vakif, "Vadesiz Hesap", "TR12 3456 **** **** 6789", 10234.56m, "TRY"),
                    new AccountDto("acc-2", BankCode.vakif, "Birikim Hesap", "TR23 4567 **** **** 7890", 13222.22m, "TRY")
                };
            }

            // MyBank: giriş yapan kullanıcının hesapları (maskesiz IBAN & bakiye DB’den)
            var accounts = await _db.MyBankAccounts
                .AsNoTracking()
                .Where(a => a.OwnerUserId == userId)
                .Select(a => new AccountDto(
                    a.Id.ToString(),
                    BankCode.mybank,
                    string.IsNullOrWhiteSpace(a.AccountName) ? "Vadesiz Hesap" : a.AccountName!,
                    a.Iban,
                    a.Balance,
                    string.IsNullOrWhiteSpace(a.Currency) ? "TRY" : a.Currency!
                ))
                .ToListAsync(ct);

            return accounts;
        }

        // ⬇⬇⬇ BURASI ÖNEMLİ: TransactionDto çakışması için OB.TransactionDto kullanıyoruz
        public Task<IEnumerable<OB.TransactionDto>> GetRecentTransactionsAsync(
            string userId, BankCode bank, int take = 5, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;

            var list = new List<OB.TransactionDto>
            {
                new("tx-1", bank, now.AddDays(-1), "Restoran", -300m, "TRY"),
                new("tx-2", bank, now.AddDays(-1), "Havale",   1000m, "TRY"),
                new("tx-3", bank, now.AddDays(-2), "Fatura",   -350m, "TRY")
            };

            return Task.FromResult(list.Take(take));
        }

        // (İstersen başka yerlerde kullanırsın)
        private static string MaskIban(string? iban)
        {
            if (string.IsNullOrWhiteSpace(iban)) return "TR** **** **** **** **** **";
            var clean = Regex.Replace(iban, @"\s+", "");
            if (clean.Length <= 10) return clean;

            var start  = clean[..6];
            var end    = clean[^4..];
            var middle = new string('*', clean.Length - 10);
            var masked = start + middle + end;
            return Regex.Replace(masked, ".{4}", "$0 ").Trim();
        }

        // ================================
        // ✅ MyBank hesap oluşturma (Servis)
        // ================================
        public async Task<MyBankAccount> CreateMyBankAccountAsync(
            string userId, string? name, string? currency, CancellationToken ct = default)
        {
            static string N(string? s) => (s ?? "").Trim();

            var cur = N(currency).ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(cur)) cur = "TRY";

            var accName = N(name);
            if (string.IsNullOrWhiteSpace(accName))
                accName = $"{cur} Hesabı";

            // IBAN & hesap numarası üret
            var rnd = new Random();

            string GenerateIban()
            {
                var digits = string.Concat(Enumerable.Range(0, 24).Select(_ => rnd.Next(0, 10)));
                return "TR" + digits; // TR + 24 hane = 26 karakter
            }

            string GenerateAccNo()
            {
                return string.Concat(Enumerable.Range(0, 14).Select(_ => rnd.Next(0, 10)));
            }

            // Benzersiz IBAN bul
            string iban;
            int tries = 0;
            do
            {
                iban = GenerateIban();
                tries++;
                if (tries > 20)
                    throw new InvalidOperationException("IBAN üretilemedi, lütfen tekrar deneyin.");
            }
            while (await _db.MyBankAccounts.AsNoTracking().AnyAsync(a => a.Iban == iban, ct));

            var entity = new MyBankAccount
            {
                OwnerUserId   = userId,
                AccountName   = accName,
                Currency      = cur,
                Balance       = 0m,
                Iban          = iban,
                AccountNumber = GenerateAccNo(), // Required ise doldurduk
                // PhoneNumber   = "",            // Required ama string.Empty defaultu null değildir
                CreatedAt     = DateTime.UtcNow,
                UpdatedAt     = DateTime.UtcNow
            };

            _db.MyBankAccounts.Add(entity);
            await _db.SaveChangesAsync(ct);

            return entity;
        }
    }
}
