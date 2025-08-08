using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Staj_Proje_1.Models
{
    public class AccountListResponse
    {
        [JsonPropertyName("Data")]
        public AccountListData Data { get; set; }
    }

    public class AccountListData
    {
        // JSON içinde "Accounts" diye geliyor, ona map edelim:
        [JsonPropertyName("Accounts")]
        public List<AccountInfo> Accounts { get; set; }
    }


   public class AccountInfo
{
    public string CurrencyCode        { get; set; } = default!;
    public string LastTransactionDate { get; set; } = default!;
    public string AccountStatus       { get; set; } = default!;
    public string IBAN                { get; set; } = default!;
    public string RemainingBalance    { get; set; } = default!;
    public string Balance             { get; set; } = default!;
    public string AccountType         { get; set; } = default!;
    public string AccountNumber       { get; set; } = default!;
}

}
