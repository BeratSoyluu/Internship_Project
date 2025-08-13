// Services/BankService.cs
#nullable enable
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using Staj_Proje_1.Models;


namespace Staj_Proje_1.Services;

public class BankService : IBankService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _cfg;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public BankService(HttpClient http, IConfiguration cfg)
    {
        _http = http;
        _cfg  = cfg;
    }

    // ------------------- Token -------------------
    public async Task<string> GetTokenAsync(CancellationToken ct = default)
    {
        var tokenUrl = _cfg["VakifBank:TokenUrl"]
            ?? throw new InvalidOperationException("VakifBank:TokenUrl yok");

        var clientId     = _cfg["VakifBank:ClientId"]     ?? throw new InvalidOperationException("ClientId yok");
        var clientSecret = _cfg["VakifBank:ClientSecret"] ?? throw new InvalidOperationException("ClientSecret yok");
        var scope        = _cfg["VakifBank:Scope"]        ?? "account";
        var consentId    = _cfg["VakifBank:ConsentId"]    ?? throw new InvalidOperationException("ConsentId yok");
        var resourceEnv  = _cfg["VakifBank:ResourceEnvironment"] ?? "sandbox";

        // Konsol logu
        Console.WriteLine("=== TOKEN REQUEST INFO ===");
        Console.WriteLine($"TokenUrl: {tokenUrl}");
        Console.WriteLine($"ClientId: {clientId}");
        Console.WriteLine($"ClientSecret: {clientSecret}");
        Console.WriteLine($"GrantType: b2b_credentials");
        Console.WriteLine($"Scope: {scope}");
        Console.WriteLine($"ConsentId: {consentId}");
        Console.WriteLine($"ResourceEnvironment: {resourceEnv}");
        Console.WriteLine("==========================");

        var form = new Dictionary<string, string>
        {
            ["client_id"]     = clientId.Trim(),
            ["client_secret"] = clientSecret.Trim(),
            ["grant_type"]    = "b2b_credentials",
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

        var full = new Uri(new Uri(baseUrl, UriKind.Absolute), path);

        using var req = new HttpRequestMessage(HttpMethod.Post, full)
        {
            // Bazı gateway’ler POST’ta boş body istemez; güvenli tarafta kalalım:
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };

        // Bankanın istediği header’lar:
        req.Headers.Add("x-ibm-client-id", _cfg["VakifBank:ClientId"]);
        req.Headers.Add("x-consent-id", _cfg["VakifBank:ConsentId"]);
        req.Headers.Add("x-resource-indicator", _cfg["VakifBank:ResourceEnvironment"]);
        req.Headers.Add("x-fapi-interaction-id", Guid.NewGuid().ToString());

        using var res  = await _http.SendAsync(req, ct);
        var       body = await res.Content.ReadAsStringAsync(ct);

        Console.WriteLine($"[DEBUG] POST {full} -> {(int)res.StatusCode}");

        if (!res.IsSuccessStatusCode)
            throw new ApplicationException($"accountList HTTP {(int)res.StatusCode} {res.ReasonPhrase}. Body: {body}");

        return JsonSerializer.Deserialize<AccountListResponse>(body, JsonOpts)
               ?? new AccountListResponse();
    }

    // ------------------- Hesap Detayı -------------------
    public async Task<AccountDetailResponse> GetAccountDetailAsync(string accessToken, string accountNumber, CancellationToken ct = default)
    {
        PrepareDefaultHeaders(accessToken);

        var baseUrl = _cfg["VakifBank:BaseUrl"]        ?? throw new InvalidOperationException("VakifBank:BaseUrl yok");
        var path    = _cfg["VakifBank:AccountDetailPath"] ?? "/accountDetail";
        var full    = new Uri(new Uri(baseUrl, UriKind.Absolute), path);

        var payload = new { AccountNumber = accountNumber };

        using var req = new HttpRequestMessage(HttpMethod.Post, full)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };

        req.Headers.Add("x-ibm-client-id", _cfg["VakifBank:ClientId"]);
        req.Headers.Add("x-consent-id", _cfg["VakifBank:ConsentId"]);
        req.Headers.Add("x-resource-indicator", _cfg["VakifBank:ResourceEnvironment"]);
        req.Headers.Add("x-fapi-interaction-id", Guid.NewGuid().ToString());

        using var res  = await _http.SendAsync(req, ct);
        var       body = await res.Content.ReadAsStringAsync(ct);

        Console.WriteLine($"[DEBUG] POST {full} -> {(int)res.StatusCode}");
        if (!res.IsSuccessStatusCode)
            throw new ApplicationException($"accountDetail HTTP {(int)res.StatusCode} {res.ReasonPhrase}. Body: {body}");

        return JsonSerializer.Deserialize<AccountDetailResponse>(body, JsonOpts)
               ?? new AccountDetailResponse();
    }

    // ------------------- Hesap Özet Bilgisi -------------------
    public async Task<AccountInfo> GetAccountInfoAsync(string accessToken, string accountNumber, CancellationToken ct = default)
    {
        var list = await GetAccountListAsync(accessToken, ct);
        var info = list.Data?.Accounts?.FirstOrDefault(a =>
            string.Equals(a.AccountNumber, accountNumber, StringComparison.OrdinalIgnoreCase));

        if (info == null)
            throw new ApplicationException($"Hesap bulunamadı: {accountNumber}");

        return info;
    }

    // ------------------- Hesap Hareketleri (tarih aralığıyla) -------------------
    public async Task<TransactionsResponse> GetAccountTransactionsAsync(
        string accessToken,
        string accountNumber,
        string startDate,   // "dd-MM-yyyy"
        string endDate,     // "dd-MM-yyyy"
        CancellationToken ct = default)
    {
        PrepareDefaultHeaders(accessToken);

        // dd-MM-yyyy -> yyyy-MM-ddTHH:mm:ss
        static string ToApiDateTime(string d, bool endOfDay)
        {
            var ok = DateTime.TryParseExact(
                d, "dd-MM-yyyy",
                new CultureInfo("tr-TR"),
                DateTimeStyles.None,
                out var dt);
            if (!ok) throw new ArgumentException($"Geçersiz tarih: {d}. Beklenen format: dd-MM-yyyy");

            dt = endOfDay ? dt.Date.AddHours(23).AddMinutes(59).AddSeconds(59) : dt.Date;
            return dt.ToString("yyyy-MM-dd'T'HH:mm:ss");
        }

        var apiStart = ToApiDateTime(startDate, endOfDay: false);
        var apiEnd   = ToApiDateTime(endDate,   endOfDay: true);

        var baseUrl = _cfg["VakifBank:BaseUrl"]
            ?? throw new InvalidOperationException("VakifBank:BaseUrl yok");
        var path = _cfg["VakifBank:AccountTransactionsPath"] ?? "/accountTransactions";
        var full = new Uri(new Uri(baseUrl, UriKind.Absolute), path);

        // Başındaki sıfırları kırpılmış varyantı da gönderiyoruz
        var accountNoNoLeading = accountNumber.TrimStart('0');

        var payload = new
        {
            accountNumber = accountNumber,   // orijinal
            accountNo     = accountNoNoLeading, // sıfırsız
            startDate     = apiStart,        // yyyy-MM-ddTHH:mm:ss
            endDate       = apiEnd
            // iban KALDIRILDI -> model eksikliği yüzünden derleme hatası veriyordu
        };

        var camel = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        Console.WriteLine("[DEBUG] URL: " + full);
        Console.WriteLine("[DEBUG] Payload: " + JsonSerializer.Serialize(payload, camel));
        Console.WriteLine("[DEBUG] Headers: " +
            string.Join("; ", _http.DefaultRequestHeaders.Select(h => $"{h.Key}={string.Join(",", h.Value)}")));

        using var req = new HttpRequestMessage(HttpMethod.Post, full)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload, camel), Encoding.UTF8, "application/json")
        };

        req.Headers.Add("x-ibm-client-id", _cfg["VakifBank:ClientId"]);
        req.Headers.Add("x-consent-id", _cfg["VakifBank:ConsentId"]);
        req.Headers.Add("x-resource-indicator", _cfg["VakifBank:ResourceEnvironment"]);
        req.Headers.Add("x-fapi-interaction-id", Guid.NewGuid().ToString());

        using var res  = await _http.SendAsync(req, ct);
        var       body = await res.Content.ReadAsStringAsync(ct);

        Console.WriteLine($"[DEBUG] POST {full} -> {(int)res.StatusCode}");
        Console.WriteLine("[DEBUG] RAW RESPONSE: " + body);

        if (!res.IsSuccessStatusCode)
            throw new ApplicationException($"accountTransactions HTTP {(int)res.StatusCode} {res.ReasonPhrase}. Body: {body}");

        return JsonSerializer.Deserialize<TransactionsResponse>(body, JsonOpts)
            ?? new TransactionsResponse();
    }




    // ------------------- Helpers -------------------
    private void PrepareDefaultHeaders(string accessToken)
    {
        _http.DefaultRequestHeaders.Clear();
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        _http.DefaultRequestHeaders.Accept.Clear();
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        // NOT: Talepe özgü x-* header’ları request üzerinde ekliyoruz (üstte ekledik).
    }
}
