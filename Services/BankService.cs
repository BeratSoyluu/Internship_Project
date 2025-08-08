using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Staj_Proje_1.Models;

namespace Staj_Proje_1.Services
{
    /// <summary>
    /// VakıfBank API çağrılarını gerçekleştiren servis.
    /// </summary>
    public class BankService : IBankService
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _cfg;

        public BankService(HttpClient http, IConfiguration cfg)
        {
            _http = http;
            _cfg  = cfg;
        }

        // -------------------------------------------------------------
        // 1) TOKEN
        // -------------------------------------------------------------
        public async Task<string> GetTokenAsync()
        {
            _http.DefaultRequestHeaders.Authorization = null;   // Header temizle

            var form = new Dictionary<string, string>
            {
                ["client_id"]     = _cfg["VakifBank:ClientId"]!.Trim(),
                ["client_secret"] = _cfg["VakifBank:ClientSecret"]!.Trim(),
                ["grant_type"]    = _cfg["VakifBank:GrantType"]!.Trim(),
                ["scope"]         = _cfg["VakifBank:Scope"]!.Trim(),
                ["consentId"]     = _cfg["VakifBank:ConsentId"]!.Trim(),
                ["resource"]      = _cfg["VakifBank:ResourceEnvironment"]!.Trim()
            };

            using var req = new HttpRequestMessage(HttpMethod.Post, "auth/oauth/v2/token")
            {
                Content = new FormUrlEncodedContent(form)
            };

            var res  = await _http.SendAsync(req);
            var body = await res.Content.ReadAsStringAsync();
            if (!res.IsSuccessStatusCode)
                throw new ApplicationException($"VakifBank token error ({res.StatusCode}): {body}");

            var dto = JsonSerializer.Deserialize<TokenResponse>(body,
                       new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
            return dto.AccessToken;
        }

        // -------------------------------------------------------------
        // 2) HESAP LISTESI
        // -------------------------------------------------------------
        public async Task<AccountListResponse> GetAccountListAsync(string token)
        {
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var res = await _http.PostAsJsonAsync("accountList", new { });
            res.EnsureSuccessStatusCode();

            return await res.Content.ReadFromJsonAsync<AccountListResponse>(
                       new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        }

        // -------------------------------------------------------------
        // 3) HESAP DETAYI (tam JSON, hâlâ lazım olursa)
        // -------------------------------------------------------------
        public async Task<AccountDetailResponse> GetAccountDetailAsync(string token, string accountNumber)
        {
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            const string path = "/accountDetail";

            var apiReq = new AccountDetailApiRequest
            {
                Header = new AccountDetailApiRequest.HeaderModel
                {
                    ChannelCode = "WEB",
                    RequestId   = Guid.NewGuid().ToString(),
                    RequestDate = DateTime.UtcNow.ToString("o")
                },
                Body = new AccountDetailApiRequest.BodyModel
                {
                    AccountNumber = accountNumber
                }
            };

            var res = await _http.PostAsJsonAsync(path, apiReq);
            res.EnsureSuccessStatusCode();

            var raw = await res.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<AccountDetailResponse>(raw,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        }

        // -------------------------------------------------------------
        // 4) HESAP DETAYI (yalnız "Data" düğümü) → AccountInfo
        // -------------------------------------------------------------
        public async Task<AccountInfo> GetAccountInfoAsync(string token, string accountNumber)
        {
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            const string path = "/accountDetail";

            var apiReq = new AccountDetailApiRequest
            {
                Header = new AccountDetailApiRequest.HeaderModel
                {
                    ChannelCode = "WEB",
                    RequestId   = Guid.NewGuid().ToString(),
                    RequestDate = DateTime.UtcNow.ToString("o")
                },
                Body = new AccountDetailApiRequest.BodyModel
                {
                    AccountNumber = accountNumber
                }
            };

            var res = await _http.PostAsJsonAsync(path, apiReq);
            res.EnsureSuccessStatusCode();

            var raw = await res.Content.ReadAsStringAsync();

            using var doc   = JsonDocument.Parse(raw);
            var dataJson    = doc.RootElement.GetProperty("Data").GetRawText();

            var info = JsonSerializer.Deserialize<AccountInfo>(
                           dataJson,
                           new JsonSerializerOptions
                           {
                               PropertyNameCaseInsensitive = true,
                               NumberHandling = JsonNumberHandling.AllowReadingFromString
                           });
            return info!;
        }

        // -------------------------------------------------------------
        // 5) HESAP HAREKETLERI
        // -------------------------------------------------------------
        public async Task<TransactionsResponse> GetAccountTransactionsAsync(string token, string accountNumber)
        {
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var res = await _http.PostAsJsonAsync("accountTransactions", new { AccountNumber = accountNumber });
            res.EnsureSuccessStatusCode();

            return await res.Content.ReadFromJsonAsync<TransactionsResponse>(
                       new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        }

        // -------------------------------------------------------------
        // 6) İÇ SINIFLAR (request şeması)
        // -------------------------------------------------------------
        private class AccountDetailApiRequest
        {
            public object Header { get; set; } = default!;
            public object Body   { get; set; } = default!;

            internal class HeaderModel
            {
                public string ChannelCode { get; set; } = default!;
                public string RequestId   { get; set; } = default!;
                public string RequestDate { get; set; } = default!;
            }

            internal class BodyModel
            {
                public string AccountNumber { get; set; } = default!;
            }
        }
    }
}
