// Services/BankService.cs
#nullable enable
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

using Staj_Proje_1.Data;            // ApplicationDbContext
using Staj_Proje_1.Models;          // MyBankAccount, MyBankTransfer
using Staj_Proje_1.Models.Dtos;     // AccountListResponse, AccountDetailResponse, TransactionsResponse, BankTransferResponse
using Staj_Proje_1.Utils;           // DecimalFlexibleConverter

namespace Staj_Proje_1.Services;

public class BankService : IBankService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _cfg;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<BankService> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly JsonSerializerOptions Camel = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public BankService(HttpClient http, IConfiguration cfg, ApplicationDbContext db, ILogger<BankService> logger)
    {
        _http   = http;
        _cfg    = cfg;
        _db     = db;
        _logger = logger;
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

        _logger.LogInformation("=== TOKEN REQUEST === Url={Url} ClientId={Id} GrantType=b2b_credentials Scope={Scope} ConsentId={Consent} Resource={Res}",
            tokenUrl, clientId, scope, consentId, resourceEnv);

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

        _logger.LogInformation("POST {Url} -> {Code}", full, (int)res.StatusCode);

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

        _logger.LogInformation("POST {Url} -> {Code}", full, (int)res.StatusCode);

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

        if (!_http.DefaultRequestHeaders.Accept.Any(h => h.MediaType == "application/json"))
            _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

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
            AccountNumber = AccountNumber,
            StartDate     = apiStart,
            EndDate       = apiEnd
        };

        _logger.LogInformation("POST {Url} Payload={Payload}", full, JsonSerializer.Serialize(payload));

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

        _logger.LogInformation("POST {Url} -> {Code}; BodyLen={Len}", full, (int)res.StatusCode, body?.Length ?? 0);

        if (!res.IsSuccessStatusCode)
            throw new ApplicationException($"accountTransactions HTTP {(int)res.StatusCode} {res.ReasonPhrase}. Body: {body}");

        var opts = new JsonSerializerOptions(JsonOpts);
        opts.Converters.Add(new DecimalFlexibleConverter());

        return JsonSerializer.Deserialize<TransactionsResponse>(body, opts)
               ?? new TransactionsResponse();
    }

    // ======================================================================
    //                   MYBANK: TEK ADIMDA TAMAMLANAN TRANSFER
    // ======================================================================

    // Basit TR IBAN doğrulayıcı
    private static bool IsValidTrIban(string? iban)
    {
        if (string.IsNullOrWhiteSpace(iban)) return false;
        iban = iban.Replace(" ", "").ToUpperInvariant();
        if (!iban.StartsWith("TR") || iban.Length != 26) return false;
        for (int i = 2; i < iban.Length; i++) if (!char.IsDigit(iban[i])) return false;
        return true;
    }

    private async Task<MyBankTransfer> CreateAndCompleteMyBankTransferAsync(
        string accountNumber, string toIban, decimal amount, string? description, CancellationToken ct)
    {
        var acc = await _db.MyBankAccounts
            .SingleOrDefaultAsync(a => a.AccountNumber == accountNumber, ct)
            ?? throw new InvalidOperationException("Hesap bulunamadı.");

        if (amount <= 0) throw new InvalidOperationException("Tutar pozitif olmalı.");
        if (!IsValidTrIban(toIban)) throw new InvalidOperationException("Geçersiz IBAN.");
        if (acc.Balance < amount) throw new InvalidOperationException("Yetersiz bakiye.");

        var now = DateTime.UtcNow;

        var tr = new MyBankTransfer
        {
            // FK alan adın her ne ise hiç kullanmadan navigasyonla set edelim:
            FromAccountId = acc.Id,
            FromAccount   = acc,
            ToIban        = toIban,
            Amount        = amount,
            Currency      = "TRY",
            Status        = TransferStatus.Completed,
            RequestedAt   = now,
            CompletedAt   = now,
            // Description/Note alanın varsa:
            // Description   = description
        };

        acc.Balance -= amount;
        _db.MyBankTransfers.Add(tr);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("MyBank transfer completed. AccNumber={Acc} Amount={Amount} To={Iban}", accountNumber, amount, toIban);
        return tr;
    }

    // IBankService imzası (BankTransferResponse döndürür)
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
        // MyBank iç transfer — accessToken burada kullanılmıyor
        var acc = await _db.MyBankAccounts
            .SingleOrDefaultAsync(a => a.AccountNumber == fromAccountNumber, ct)
            ?? throw new InvalidOperationException("Hesap bulunamadı.");

        if (amount <= 0) throw new InvalidOperationException("Tutar pozitif olmalı.");
        if (acc.Balance < amount) throw new InvalidOperationException("Yetersiz bakiye.");
        if (!IsValidTrIban(toIban)) throw new InvalidOperationException("Geçersiz IBAN.");

        var now = DateTime.UtcNow;

        var tr = new MyBankTransfer
        {
            FromAccountId = acc.Id,
            FromAccount   = acc,
            ToIban        = toIban,
            ToName        = toName ?? string.Empty,
            Amount        = amount,
            Currency      = string.IsNullOrWhiteSpace(currency) ? "TRY" : currency,
            Description   = description,
            Status = TransferStatus.Completed,
            RequestedAt   = now,
            CompletedAt   = now
        };

        acc.Balance -= amount;
        _db.MyBankTransfers.Add(tr);
        await _db.SaveChangesAsync(ct);

        return new BankTransferResponse
        {
            Success           = true,
            Reference         = tr.Id.ToString(),
            StatusCode        = "200",
            StatusDescription = "Transfer completed",
            RawBody           = JsonSerializer.Serialize(tr)
        };
    }


    // ------------------- Helpers -------------------
    private void PrepareDefaultHeaders(string accessToken)
    {
        _http.DefaultRequestHeaders.Clear();
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        _http.DefaultRequestHeaders.Accept.Clear();
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        // x-* header’lar request bazında ekleniyor.
    }
}
