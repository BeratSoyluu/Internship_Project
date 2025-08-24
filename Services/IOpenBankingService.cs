using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Staj_Proje_1.Models;                     // MyBankAccount
using OB = Staj_Proje_1.Models.OpenBanking;   // OpenBanking modelleri (BankDto, AccountDto, ...)

namespace Staj_Proje_1.Services
{
    public interface IOpenBankingService
    {
        Task<IEnumerable<OB.BankDto>> GetLinkedBanksAsync(
            string userId,
            CancellationToken ct = default
        );

        Task<OB.BankDto> LinkBankAsync(
            string userId,
            OB.LinkBankRequest req,
            CancellationToken ct = default
        );

        Task<IEnumerable<OB.AccountDto>> GetAccountsAsync(
            string userId,
            OB.BankCode bank,
            CancellationToken ct = default
        );

        // DİKKAT: TransactionDto çakışmasını önlemek için OB.TransactionDto kullanıyoruz
        Task<IEnumerable<OB.TransactionDto>> GetRecentTransactionsAsync(
            string userId,
            OB.BankCode bank,
            int take = 5,
            CancellationToken ct = default
        );

        // MyBank: IBAN otomatik, bakiye = 0
        Task<MyBankAccount> CreateMyBankAccountAsync(
            string userId,
            string? name,
            string? currency,
            CancellationToken ct = default
        );
    }
}
