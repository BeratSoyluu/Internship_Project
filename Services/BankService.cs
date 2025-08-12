// Services/BankService.cs
#nullable enable
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using Staj_Proje_1.Models;
using Staj_Proje_1.Models.Dtos;

namespace Staj_Proje_1.Services;

public class BankService : IBankService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _cfg;

    public BankService(HttpClient http, IConfiguration cfg)
    {
        _http = http;
        _cfg = cfg;
    }

    // ------------------- Token -------------------
    public async Task<string> GetTokenAsync(CancellationToken ct = default)
    {
        var tokenUrl = _cfg["VakifBank:TokenUrl"];
        if (string.IsNullOrWhiteSpace(tokenUrl))
            throw new InvalidOperationException("VakifBank:TokenUrl appsettings.json içinde tanımlı değil.");

        var form = new Dictionary<string, string>
        {
            ["client_id"]     = _cfg["VakifBank:ClientId"]!.Trim(),
            ["client_secret"] = _cfg["VakifBank:ClientSecret"]!.Trim(),
            ["grant_type"]    = _cfg["VakifBank:GrantType"]!.Trim(),
            ["scope"]         = _cfg["VakifBank:Scope"]!.Trim(),
            ["consentId"]     = _cfg["VakifBank:ConsentId"]!.Trim(),
            ["resource"]      = _cfg["VakifBank:ResourceEnvironment"]!.Trim()
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, tokenUrl)
        {
            Content = new FormUrlEncodedContent(form)
        };

        using var res  = await _http.SendAsync(req, ct);
        var       body = await res.Content.ReadAsStringAsync(ct);

        if (!res.IsSuccessStatusCode)
            throw new ApplicationException($"token HTTP {(int)res.StatusCode} {res.ReasonPhrase}. Body: {body}");

        using var doc = JsonDocument.Parse(body);
        var accessToken = doc.RootElement.TryGetProperty("access_token", out var at)
            ? at.GetString()
            : null;

        if (string.IsNullOrWhiteSpace(accessToken))
            throw new ApplicationException("Token yanıtında 'access_token' bulunamadı.");

        return accessToken!;
    }

    // ------------------- Hesap Listesi -------------------
    public async Task<AccountListResponse> GetAccountListAsync(string accessToken, CancellationToken ct = default)
    {
        PrepareDefaultHeaders(accessToken);

        var baseUrl = _cfg["VakifBank:BaseUrl"] ?? throw new InvalidOperationException("VakifBank:BaseUrl yok");
        var path    = _cfg["VakifBank:AccountsPath"] ?? throw new InvalidOperationException("VakifBank:AccountsPath yok");

        // Mutlak URL oluştur
        var full = new Uri(new Uri(baseUrl, UriKind.Absolute), path); // kök + /versiyonlu/path

        using var req = new HttpRequestMessage(HttpMethod.Post, full);

        // VakıfBank gateway genelde bu header’ları bekler (eksikse bazen 404 döndürür):
        req.Headers.Add("x-ibm-client-id", _cfg["VakifBank:ClientId"]);
        req.Headers.Add("x-consent-id", _cfg["VakifBank:ConsentId"]);
        req.Headers.Add("x-resource-indicator", _cfg["VakifBank:ResourceEnvironment"]);
        // req.Headers.Add("x-fapi-interaction-id", Guid.NewGuid().ToString()); // gerekiyorsa aç

        using var res  = await _http.SendAsync(req, ct);
        var       body = await res.Content.ReadAsStringAsync(ct);

        // Teşhis için log
        Console.WriteLine($"[DEBUG] GET {full} -> {(int)res.StatusCode}");

        if (!res.IsSuccessStatusCode)
            throw new ApplicationException($"accountList HTTP {(int)res.StatusCode} {res.ReasonPhrase}. Body: {body}");

        return JsonSerializer.Deserialize<AccountListResponse>(body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? new AccountListResponse();
    }



    // ------------------- Hesap Detayı -------------------
    public async Task<AccountDetailResponse> GetAccountDetailAsync(string accessToken, string accountNumber, CancellationToken ct = default)
    {
        PrepareDefaultHeaders(accessToken);

        var url = "account/detail"; // Gerekiyorsa gerçek path ile değiştir
        var payload = new { AccountNumber = accountNumber };

        using var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json")
        };

        using var res  = await _http.SendAsync(req, ct);
        var       body = await res.Content.ReadAsStringAsync(ct);

        if (!res.IsSuccessStatusCode)
            throw new ApplicationException($"accountDetail HTTP {(int)res.StatusCode} {res.ReasonPhrase}. Body: {body}");

        return JsonSerializer.Deserialize<AccountDetailResponse>(
            body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        ) ?? new AccountDetailResponse();
    }

    // ------------------- Hesap Özet Bilgisi -------------------
    public async Task<AccountInfo> GetAccountInfoAsync(string accessToken, string accountNumber, CancellationToken ct = default)
    {
        // Basit implementasyon: listeyi al, numaraya göre bul
        var list = await GetAccountListAsync(accessToken, ct);
        var info = list.Data?.Accounts?.FirstOrDefault(a =>
            string.Equals(a.AccountNumber, accountNumber, StringComparison.OrdinalIgnoreCase));

        if (info == null)
            throw new ApplicationException($"Hesap bulunamadı: {accountNumber}");

        return info;
    }

    // ------------------- Hesap Hareketleri -------------------
    public async Task<TransactionsResponse> GetAccountTransactionsAsync(string accessToken, string accountNumber, CancellationToken ct = default)
    {
        PrepareDefaultHeaders(accessToken);

        // Kurgu path: gereğine göre değiştir
        var url = $"accounts/{accountNumber}/transactions";

        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        using var res  = await _http.SendAsync(req, ct);
        var       body = await res.Content.ReadAsStringAsync(ct);

        if (!res.IsSuccessStatusCode)
            throw new ApplicationException($"transactions HTTP {(int)res.StatusCode} {res.ReasonPhrase}. Body: {body}");

        return JsonSerializer.Deserialize<TransactionsResponse>(
            body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        ) ?? new TransactionsResponse();
    }

    // ------------------- Helpers -------------------
    private void PrepareDefaultHeaders(string accessToken)
    {
        _http.DefaultRequestHeaders.Clear();
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        _http.DefaultRequestHeaders.Accept.Clear();
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Gerekirse zorunlu header'ları aç:
        // _http.DefaultRequestHeaders.Add("x-consent-id", _cfg["VakifBank:ConsentId"]);
        // _http.DefaultRequestHeaders.Add("x-ibm-client-id", _cfg["VakifBank:ClientId"]);
        // _http.DefaultRequestHeaders.Add("x-resource-indicator", _cfg["VakifBank:ResourceEnvironment"]);
        // _http.DefaultRequestHeaders.Add("x-fapi-interaction-id", Guid.NewGuid().ToString());
    }
}
