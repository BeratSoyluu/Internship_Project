using System.Threading.Tasks;
using Staj_Proje_1.Models;

namespace Staj_Proje_1.Services
{
    /// <summary>
    /// VakıfBank API çağrılarını soyutlayan servis arayüzü.
    /// </summary>
    public interface IBankService
    {
        // ------------------- Yetkilendirme -------------------
        /// <summary>OAuth2 erişim tokenı alır.</summary>
        Task<string> GetTokenAsync();

        // ------------------- Hesaplar ------------------------
        /// <summary>Kullanıcının tüm hesaplarının özet listesini döner.</summary>
        Task<AccountListResponse> GetAccountListAsync(string token);

        /// <summary>
        /// Tek bir hesabın **tam** JSON yanıtını döner
        /// (dıştaki "Data" zarfı dâhil).
        /// </summary>
        Task<AccountDetailResponse> GetAccountDetailAsync(string token, string accountNumber);

        /// <summary>
        /// Tek bir hesabın **yalnızca "Data"** düğümünü
        /// <see cref="AccountInfo"/> tipinde döner.
        /// </summary>
        Task<AccountInfo> GetAccountInfoAsync(string token, string accountNumber);

        // ------------------- Hareketler ----------------------
        /// <summary>Belirtilen hesabın işlem/hareket listesini döner.</summary>
        Task<TransactionsResponse> GetAccountTransactionsAsync(string token, string accountNumber);
    }
}
