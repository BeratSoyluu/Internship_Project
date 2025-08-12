// Services/IBankService.cs
#nullable enable
using System.Threading;
using System.Threading.Tasks;
using Staj_Proje_1.Models;
using Staj_Proje_1.Models.Dtos;


namespace Staj_Proje_1.Services;

/// <summary>
/// VakıfBank API çağrılarını soyutlayan servis arayüzü.
/// </summary>
public interface IBankService
{
    // ------------------- Yetkilendirme -------------------

    /// <summary>Bankadan access token alır.</summary>
    Task<string> GetTokenAsync(CancellationToken ct = default);

    // ------------------- Hesaplar ------------------------

    /// <summary>Kullanıcıya ait hesap listesini döner.</summary>
    Task<AccountListResponse> GetAccountListAsync(string accessToken, CancellationToken ct = default);

    /// <summary>Tek bir hesaba ait detayları döner.</summary>
    Task<AccountDetailResponse> GetAccountDetailAsync(string accessToken, string accountNumber, CancellationToken ct = default);

    /// <summary>Hesap özet bilgisi (numara, IBAN, para birimi, bakiye vb.).</summary>
    Task<AccountInfo> GetAccountInfoAsync(string accessToken, string accountNumber, CancellationToken ct = default);

    // ------------------- Hareketler ----------------------

    /// <summary>Belirtilen hesabın işlem/hareket listesini döner.</summary>
    Task<TransactionsResponse> GetAccountTransactionsAsync(
    string accessToken,
    string accountNumber,
    CancellationToken ct = default);

}
