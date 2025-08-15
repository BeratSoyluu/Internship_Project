// Services/BankService.cs
#nullable enable
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using Staj_Proje_1.Models;
using Staj_Proje_1.Models.Dtos; // TransactionsResponse, BankTransferResponse
using Staj_Proje_1.Utils;


namespace Staj_Proje_1.Services;

public class BankService : IBankService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _cfg;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly JsonSerializerOptions Camel = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public BankService(HttpClient http, IConfiguration cfg)
    {
        _http  = http;
        _cfg   = cfg;
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
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };

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
        string AccountNumber,
        string StartDate,   // "dd-MM-yyyy"
        string EndDate,     // "dd-MM-yyyy"
        CancellationToken ct = default)
    {
        PrepareDefaultHeaders(accessToken);
        // İsteğe bağlı: Accept'i garanti altına al
        if (!_http.DefaultRequestHeaders.Accept.Any(h => h.MediaType == "application/json"))
            _http.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

        static string ToApiDateTime(string d, bool endOfDay)
        {
            var ok = DateTime.TryParseExact(
                d, "dd-MM-yyyy",
                new CultureInfo("tr-TR"),
                DateTimeStyles.None,
                out var dt);

            if (!ok)
                throw new ArgumentException($"Geçersiz tarih: {d}. Beklenen format: dd-MM-yyyy");

            var local = endOfDay
                ? dt.Date.AddHours(23).AddMinutes(59).AddSeconds(59)
                : dt.Date;

            var dto = new DateTimeOffset(local, TimeSpan.FromHours(3));
            return dto.ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture);
        }

        var apiStart = ToApiDateTime(StartDate, endOfDay: false);
        var apiEnd   = ToApiDateTime(EndDate,   endOfDay: true);

        var baseUrl = _cfg["VakifBank:BaseUrl"]
            ?? throw new InvalidOperationException("VakifBank:BaseUrl yok");
        var path = _cfg["VakifBank:AccountTransactionsPath"] ?? "/accountTransactions";
        var full = new Uri(new Uri(baseUrl, UriKind.Absolute), path);

        var payload = new
        {
            AccountNumber = AccountNumber, // PascalCase
            StartDate     = apiStart,
            EndDate       = apiEnd
        };

        Console.WriteLine("[DEBUG] URL: " + full);
        Console.WriteLine("[DEBUG] Payload: " + JsonSerializer.Serialize(payload));
        Console.WriteLine("[DEBUG] Headers: " +
            string.Join("; ", _http.DefaultRequestHeaders.Select(h => $"{h.Key}={string.Join(",", h.Value)}")));

        using var req = new HttpRequestMessage(HttpMethod.Post, full)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };

        req.Headers.Add("x-ibm-client-id", _cfg["VakifBank:ClientId"]);
        req.Headers.Add("x-consent-id", _cfg["VakifBank:ConsentId"]);
        req.Headers.Add("x-resource-indicator", _cfg["VakifBank:ResourceEnvironment"]);
        req.Headers.Add("x-fapi-interaction-id", Guid.NewGuid().ToString());

        using var res  = await _http.SendAsync(req, ct);
        var body = await res.Content.ReadAsStringAsync(ct);

        Console.WriteLine($"[DEBUG] POST {full} -> {(int)res.StatusCode}");
        Console.WriteLine("[DEBUG] RAW RESPONSE: " + body);

        // Hata kontrolü (body'yi loglayıp anlaşılır exception)
        if (!res.IsSuccessStatusCode)
            throw new ApplicationException($"accountTransactions HTTP {(int)res.StatusCode} {res.ReasonPhrase}. Body: {body}");

        // Amount için esnek converter ekle
        var opts = new JsonSerializerOptions(JsonOpts);
        opts.Converters.Add(new DecimalFlexibleConverter());

        return JsonSerializer.Deserialize<TransactionsResponse>(body, opts)
            ?? new TransactionsResponse();
    }


    // ------------------- Para Transferi (gerçek çağrı) -------------------
    public async Task<BankTransferResponse> SendTransferAsync(
        string accessToken,
        string fromAccountNumber,
        string toIban,
        string toName,
        decimal amount,
        string currency,
        string? description,
        CancellationToken ct = default)
    {
        PrepareDefaultHeaders(accessToken);

        var baseUrl = _cfg["VakifBank:BaseUrl"]
            ?? throw new InvalidOperationException("VakifBank:BaseUrl yok");
        var path = _cfg["VakifBank:MoneyTransferPath"] ?? "/moneyTransfer";
        var full = new Uri(new Uri(baseUrl, UriKind.Absolute), path);

        var payload = new
        {
            // Gönderen
            fromAccountNumber = fromAccountNumber,
            fromAccountNo     = fromAccountNumber?.TrimStart('0'),

            // Alıcı
            toIban        = toIban,
            receiverIban  = toIban,
            toName        = toName,
            receiverName  = toName,

            // Tutar
            amount   = amount,
            currency = (currency ?? "TRY").ToUpperInvariant(),

            description = description
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, full)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload, Camel), Encoding.UTF8, "application/json")
        };

        req.Headers.Add("x-ibm-client-id", _cfg["VakifBank:ClientId"]);
        req.Headers.Add("x-consent-id", _cfg["VakifBank:ConsentId"]);
        req.Headers.Add("x-resource-indicator", _cfg["VakifBank:ResourceEnvironment"]);
        req.Headers.Add("x-fapi-interaction-id", Guid.NewGuid().ToString());

        Console.WriteLine("[TRANSFER][REQ] " + full);
        Console.WriteLine("[TRANSFER][PAYLOAD] " + JsonSerializer.Serialize(payload, Camel));

        using var res = await _http.SendAsync(req, ct);
        var body = await res.Content.ReadAsStringAsync(ct);

        Console.WriteLine($"[TRANSFER][RES] {(int)res.StatusCode}");
        Console.WriteLine("[TRANSFER][BODY] " + body);

        var result = new BankTransferResponse { RawBody = body };

        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
            var root = doc.RootElement;

            if (root.TryGetProperty("Header", out var hdr))
            {
                if (hdr.TryGetProperty("StatusCode", out var sc)) result.StatusCode = sc.GetString();
                if (hdr.TryGetProperty("StatusDescription", out var sd)) result.StatusDescription = sd.GetString();
            }

            // Olası referans alan adları
            foreach (var key in new[] { "reference", "referenceNo", "referenceNumber", "transactionReference", "transferId", "orderId" })
            {
                if (root.TryGetProperty(key, out var v)) { result.Reference = v.GetString(); break; }
                if (root.TryGetProperty("Body", out var b) && b.TryGetProperty(key, out var vb)) { result.Reference = vb.GetString(); break; }
            }

            result.Success = res.IsSuccessStatusCode &&
                             (result.Reference != null ||
                              string.IsNullOrEmpty(result.StatusCode) ||
                              result.StatusCode.StartsWith("ACBH0000", StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            result.Success = res.IsSuccessStatusCode;
        }

        if (!res.IsSuccessStatusCode)
        {
            result.StatusCode ??= ((int)res.StatusCode).ToString(CultureInfo.InvariantCulture);
            result.StatusDescription ??= res.ReasonPhrase;
        }

        return result;
    }

    // ------------------- Helpers -------------------
    private void PrepareDefaultHeaders(string accessToken)
    {
        _http.DefaultRequestHeaders.Clear();
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        _http.DefaultRequestHeaders.Accept.Clear();
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        // x-* header’ları request bazında ekleniyor (yukarıda).
    }
}
