using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Staj_Proje_1.Data;
using Staj_Proje_1.Models.OpenBanking;

namespace Staj_Proje_1.Services;

public class OpenBankingService : IOpenBankingService
{
    private readonly ApplicationDbContext _db;

    public OpenBankingService(ApplicationDbContext db) => _db = db;

    public async Task<IEnumerable<BankDto>> GetLinkedBanksAsync(string userId, CancellationToken ct = default)
    {
        var hasMyBank = await _db.MyBankAccounts
            .AsNoTracking()
            .AnyAsync(a => a.OwnerUserId == userId, ct);

        // Hesap yoksa null döner; ?? 0m ile 0'a düşürürüz
        var myBankBalance = await _db.MyBankAccounts
            .AsNoTracking()
            .Where(a => a.OwnerUserId == userId)
            .SumAsync(a => (decimal?)a.Balance, ct) ?? 0m;

        var vakifBalance = 12345.67m; // şimdilik dummy

        return new[]
        {
            new BankDto("vakif-1", "VakıfBank", BankCode.vakif, vakifBalance, true),
            new BankDto("mybank-1", "MyBank",  BankCode.mybank, myBankBalance, hasMyBank)
        };
    }

    public Task<BankDto> LinkBankAsync(string userId, LinkBankRequest req, CancellationToken ct = default)
    {
        var dto = req.BankCode switch
        {
            BankCode.vakif  => new BankDto("vakif-1","VakıfBank", BankCode.vakif, 0m, true),
            BankCode.mybank => new BankDto("mybank-1","MyBank",  BankCode.mybank, 0m, true),
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

        // === MyBank: giriş yapan kullanıcının TÜM hesapları (IBAN maskesiz, bakiye DB'den) ===
        var accounts = await _db.MyBankAccounts
            .AsNoTracking()
            .Where(a => a.OwnerUserId == userId)
            .Select(a => new AccountDto(
                a.Id.ToString(),
                BankCode.mybank,
                string.IsNullOrWhiteSpace(a.AccountName) ? "Vadesiz Hesap" : a.AccountName!,
                a.Iban,                                              // ✅ maskesiz IBAN
                a.Balance,                                           // ✅ DB'deki bakiye
                string.IsNullOrWhiteSpace(a.Currency) ? "TRY" : a.Currency!
            ))
            .ToListAsync(ct);

        return accounts;
    }

    public Task<IEnumerable<TransactionDto>> GetRecentTransactionsAsync(string userId, BankCode bank, int take = 5, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var list = new List<TransactionDto>
        {
            new("tx-1", bank, now.AddDays(-1), "Restoran", -300m, "TRY"),
            new("tx-2", bank, now.AddDays(-1), "Havale",   1000m, "TRY"),
            new("tx-3", bank, now.AddDays(-2), "Fatura",   -350m, "TRY")
        };
        return Task.FromResult(list.Take(take));
    }

    // ibanı maskelemek için kullanıyoruz.
    private static string MaskIban(string? iban)
    {
        if (string.IsNullOrWhiteSpace(iban)) return "TR** **** **** **** **** **";
        var clean = Regex.Replace(iban, @"\s+", "");
        if (clean.Length <= 10) return clean;

        var start = clean[..6];
        var end   = clean[^4..];
        var middle = new string('*', clean.Length - 10);
        var masked = start + middle + end;

        // 4'lü gruplar halinde boşlukla biçimlendir
        return Regex.Replace(masked, ".{4}", "$0 ").Trim();
    }
}
