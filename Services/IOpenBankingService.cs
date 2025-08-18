using Staj_Proje_1.Models.OpenBanking;

namespace Staj_Proje_1.Services;

public interface IOpenBankingService
{
    Task<IEnumerable<BankDto>> GetLinkedBanksAsync(string userId, CancellationToken ct = default);
    Task<BankDto> LinkBankAsync(string userId, LinkBankRequest req, CancellationToken ct = default);
    Task<IEnumerable<AccountDto>> GetAccountsAsync(string userId, BankCode bank, CancellationToken ct = default);
    Task<IEnumerable<TransactionDto>> GetRecentTransactionsAsync(string userId, BankCode bank, int take = 5, CancellationToken ct = default);
}
