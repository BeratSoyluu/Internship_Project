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
        var tokenUrl = _cfg["VakifBank:TokenUrl"] 
            ?? throw new InvalidOperationException("VakifBank:TokenUrl yok");

        var clientId     = _cfg["VakifBank:ClientId"]!;
        var clientSecret = _cfg["VakifBank:ClientSecret"]!;
        var scope        = _cfg["VakifBank:Scope"] ?? "account";
        var consentId    = _cfg["VakifBank:ConsentId"]!;
        var resourceEnv  = _cfg["VakifBank:ResourceEnvironment"] ?? "sandbox";

        // Konsol logu
        Console.WriteLine("=== TOKEN REQUEST INFO ===");
        Console.WriteLine($"TokenUrl: {tokenUrl}");
        Console.WriteLine($"ClientId: {clientId}");
        Console.WriteLine($"ClientSecret: {clientSecret}");
        Console.WriteLine($"GrantType: b2b_credentials"); // Artık sabit
        Console.WriteLine($"Scope: {scope}");
        Console.WriteLine($"ConsentId: {consentId}");
        Console.WriteLine($"ResourceEnvironment: {resourceEnv}");
        Console.WriteLine("==========================");

        var form = new Dictionary<string, string>
        {
            ["client_id"]     = clientId.Trim(),
            ["client_secret"] = clientSecret.Trim(),
            ["grant_type"]    = "b2b_credentials", // Sabit olarak set ettik
            ["scope"]         = scope.Trim(),
            ["consentId"]     = consentId.Trim(),
            ["resource"]      = resourceEnv.Trim()
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

        var baseUrl = _cfg["VakifBank:BaseUrl"] 
            ?? throw new InvalidOperationException("VakifBank:BaseUrl yok");
        var path = _cfg["VakifBank:AccountsPath"] 
            ?? throw new InvalidOperationException("VakifBank:AccountsPath yok");

        // Mutlak URL oluştur
        var full = new Uri(new Uri(baseUrl, UriKind.Absolute), path);

        using var req = new HttpRequestMessage(HttpMethod.Post, full)
        {
            // Bazı gateway’ler POST’ta boş body istemez; güvenli tarafta kalalım:
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };

        // Zorunlu/önerilen header’lar
        req.Headers.Add("x-ibm-client-id", _cfg["VakifBank:ClientId"]);
        req.Headers.Add("x-consent-id", _cfg["VakifBank:ConsentId"]);
        req.Headers.Add("x-resource-indicator", _cfg["VakifBank:ResourceEnvironment"]);
        req.Headers.Add("x-fapi-interaction-id", Guid.NewGuid().ToString());

        // req.Headers.Add("x-fapi-interaction-id", Guid.NewGuid().ToString()); // gerekiyorsa aç

        using var res = await _http.SendAsync(req, ct);
        var body = await res.Content.ReadAsStringAsync(ct);

        // Teşhis için log
        Console.WriteLine($"[DEBUG] POST {full} -> {(int)res.StatusCode}");

        

        return JsonSerializer.Deserialize<AccountListResponse>(
                body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            )
            ?? new AccountListResponse();
    }




    public async Task<AccountDetailResponse> GetAccountDetailAsync(string accessToken, string accountNumber, CancellationToken ct = default)
    {
        PrepareDefaultHeaders(accessToken);

        var baseUrl = _cfg["VakifBank:BaseUrl"]!;
        var path    = _cfg["VakifBank:AccountDetailPath"] ?? "/accountDetail";
        var full    = new Uri(new Uri(baseUrl), path);

        var payload = new { AccountNumber = accountNumber };

        using var req = new HttpRequestMessage(HttpMethod.Post, full)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };

        req.Headers.Add("x-ibm-client-id", _cfg["VakifBank:ClientId"]);
        req.Headers.Add("x-consent-id", _cfg["VakifBank:ConsentId"]);
        req.Headers.Add("x-resource-indicator", _cfg["VakifBank:ResourceEnvironment"]);

        using var res  = await _http.SendAsync(req, ct);
        var       body = await res.Content.ReadAsStringAsync(ct);

        Console.WriteLine($"[DEBUG] POST {full} -> {(int)res.StatusCode}");
        if (!res.IsSuccessStatusCode)
            throw new ApplicationException($"accountDetail HTTP {(int)res.StatusCode} {res.ReasonPhrase}. Body: {body}");

        return JsonSerializer.Deserialize<AccountDetailResponse>(body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new AccountDetailResponse();
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

    public async Task<TransactionsResponse> GetAccountTransactionsAsync(string accessToken, string accountNumber, CancellationToken ct = default)
    {
        PrepareDefaultHeaders(accessToken);

        var baseUrl = _cfg["VakifBank:BaseUrl"]!;
        var path    = _cfg["VakifBank:TransactionsPath"] ?? "/accountTransactions";
        var full    = new Uri(new Uri(baseUrl), path);

        // Örnek tarih aralığı: son 30 gün
        var end   = DateTime.UtcNow;
        var start = end.AddDays(-30);

        var payload = new {
            AccountNumber = accountNumber,
            StartDate = start.ToString("yyyy-MM-ddTHH:mm:ss"),
            EndDate   = end.ToString("yyyy-MM-ddTHH:mm:ss")
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, full)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };

        req.Headers.Add("x-ibm-client-id", _cfg["VakifBank:ClientId"]);
        req.Headers.Add("x-consent-id", _cfg["VakifBank:ConsentId"]);
        req.Headers.Add("x-resource-indicator", _cfg["VakifBank:ResourceEnvironment"]);

        using var res  = await _http.SendAsync(req, ct);
        var       body = await res.Content.ReadAsStringAsync(ct);

        Console.WriteLine($"[DEBUG] POST {full} -> {(int)res.StatusCode}");
        if (!res.IsSuccessStatusCode)
            throw new ApplicationException($"transactions HTTP {(int)res.StatusCode} {res.ReasonPhrase}. Body: {body}");

        return JsonSerializer.Deserialize<TransactionsResponse>(body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new TransactionsResponse();
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
