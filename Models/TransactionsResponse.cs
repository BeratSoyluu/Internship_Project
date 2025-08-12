using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Staj_Proje_1.Models.Dtos
{
    public class TransactionsResponse
    {
        [JsonPropertyName("Data")]
        public TransactionsData Data { get; set; } = new();
    }

    public class TransactionsData
    {
        // Banka API’sinden gelen JSON alan adını kontrol et
        // Gerekirse "AccountTransactions" yerine gerçek adı yaz
        [JsonPropertyName("AccountTransactions")]
        public List<TransactionDto> AccountTransactions { get; set; } = new();
    }

    public class TransactionDto
    {
        [JsonPropertyName("TransactionDate")]
        public string TransactionDate { get; set; } = "";

        [JsonPropertyName("Description")]
        public string Description { get; set; } = "";

        [JsonPropertyName("Amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("Currency")]
        public string Currency { get; set; } = "";

        // Gerekirse diğer alanları ekle
        [JsonPropertyName("TransactionCode")]
        public string? TransactionCode { get; set; }

        [JsonPropertyName("Balance")]
        public decimal? Balance { get; set; }

        [JsonPropertyName("TransactionName")]
        public string? TransactionName { get; set; }

        [JsonPropertyName("TransactionId")]
        public string? TransactionId { get; set; }
    }
}
