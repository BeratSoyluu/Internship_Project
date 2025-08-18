using Staj_Proje_1.Models.OpenBanking;

namespace Staj_Proje_1.Services;

public class OpenBankingService : IOpenBankingService
{
    private readonly IBankService _vakif; // Senin mevcut VakıfBank servisin

    public OpenBankingService(IBankService vakif)
    {
        _vakif = vakif;
    }

    public Task<IEnumerable<BankDto>> GetLinkedBanksAsync(string userId, CancellationToken ct = default)
    {
        // TODO: Gerçekte DB'den oku (userId'ye bağlı bağlantılar)
        var list = new List<BankDto>
        {
            new("vakif-1","VakıfBank", BankCode.vakif, 12345.67m, true),
            new("mybank-1","MyBank", BankCode.mybank, 0m, false)
        };
        return Task.FromResult(list.AsEnumerable());
    }

    public Task<BankDto> LinkBankAsync(string userId, LinkBankRequest req, CancellationToken ct = default)
    {
        // TODO: Gerçekte OAuth/consent akışını başlat, sonra DB'ye kaydet
        var dto = req.BankCode switch
        {
            BankCode.vakif => new BankDto("vakif-1","VakıfBank", BankCode.vakif, 0m, true),
            BankCode.mybank => new BankDto("mybank-1","MyBank", BankCode.mybank, 0m, true),
            _ => throw new ArgumentOutOfRangeException()
        };
        return Task.FromResult(dto);
    }

    public async Task<IEnumerable<AccountDto>> GetAccountsAsync(string userId, BankCode bank, CancellationToken ct = default)
    {
        if (bank == BankCode.vakif)
        {
            // TODO: Burayı gerçek VakıfBank çağrınla değiştir
            // var token = await _vakif.GetTokenAsync(ct);
            // var accountList = await _vakif.GetAccountListAsync(token, ct);
            // Map -> AccountDto
            return new[]
            {
                new AccountDto("acc-1", BankCode.vakif, "Vadesiz Hesap", "TR12 3456 **** **** 6789", 10234.56m, "TRY"),
                new AccountDto("acc-2", BankCode.vakif, "Birikim Hesap", "TR23 4567 **** **** 7890", 13222.22m, "TRY")
            };
        }
        else
        {
            // MyBank henüz dummy
            return Array.Empty<AccountDto>();
        }
    }

    public Task<IEnumerable<TransactionDto>> GetRecentTransactionsAsync(string userId, BankCode bank, int take = 5, CancellationToken ct = default)
    {
        // Dummy veriler
        var now = DateTime.UtcNow;
        var list = new List<TransactionDto>
        {
            new("tx-1", bank, now.AddDays(-1), "Restoran", -300m, "TRY"),
            new("tx-2", bank, now.AddDays(-1), "Havale", +1000m, "TRY"),
            new("tx-3", bank, now.AddDays(-2), "Fatura Ödemesi", -350m, "TRY")
        }.Take(take);
        return Task.FromResult(list);
    }
}
