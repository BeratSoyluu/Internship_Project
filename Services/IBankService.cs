// Services/IBankService.cs
using System.Threading.Tasks;
using Staj_Proje_1.Models;

namespace Staj_Proje_1.Services
{
    /// <summary>VakıfBank API çağrılarını soyutlayan servis arayüzü.</summary>
    public interface IBankService
    {
        // ------------------- Yetkilendirme -------------------
        Task<string> GetTokenAsync();

        // ------------------- Hesaplar ------------------------
        Task<AccountListResponse> GetAccountListAsync(string token);
        Task<AccountDetailResponse> GetAccountDetailAsync(string token, string accountNumber);
        Task<AccountInfo> GetAccountInfoAsync(string token, string accountNumber);

        // ------------------- Hareketler ----------------------
        Task<TransactionsResponse> GetAccountTransactionsAsync(string token, string accountNumber);
    }
}
