using System.Threading;
using System.Threading.Tasks;
using Staj_Proje_1.Models;
using Staj_Proje_1.Models.Dtos;


namespace Staj_Proje_1.Services
{
    public interface IBankService
    {
        Task<string> GetTokenAsync(CancellationToken ct = default);

        Task<AccountListResponse> GetAccountListAsync(
            string accessToken,
            CancellationToken ct = default);

        Task<AccountDetailResponse> GetAccountDetailAsync(
            string accessToken,
            string accountNumber,
            CancellationToken ct = default);

        // <-- Controller'ın çağırdığı method
        Task<AccountInfo> GetAccountInfoAsync(
            string accessToken,
            string accountNumber,
            CancellationToken ct = default);

        Task<TransactionsResponse> GetAccountTransactionsAsync(
            string accessToken,
            string accountNumber,
            string startDate,   // "dd-MM-yyyy"
            string endDate,     // "dd-MM-yyyy"
            CancellationToken ct = default);

        Task<BankTransferResponse> SendTransferAsync(
            string accessToken,
            string fromAccountNumber,
            string toIban,
            string toName,
            decimal amount,
            string currency,
            string? description,
            CancellationToken ct = default);

    }
}
