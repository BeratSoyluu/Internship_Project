using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Staj_Proje_1.Models
{
    public class TransactionsResponse
    {
        [JsonPropertyName("Data")]
        public TransactionsData Data { get; set; } = new();
    }

    public class TransactionsData
    {
        // JSON’da dönülen array’in property adı farklı olabilir, 
        // örneğin "AccountTransactions" ya da "TransactionList" vs.
        // Postman cevabınıza bakarak aşağıdakini güncelleyin:

        [JsonPropertyName("AccountTransactions")]
        public List<Transaction> AccountTransactions { get; set; } = new();
    }

    public class Transaction
    {
        // Aşağıdakileri Postman’daki her transaction objesinin alanlarına göre güncelleyin:
        [JsonPropertyName("TransactionDate")]
        public string TransactionDate { get; set; } = "";

        [JsonPropertyName("Description")]
        public string Description { get; set; } = "";

        [JsonPropertyName("Amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("Currency")]
        public string Currency { get; set; } = "";

        // Gerekiyorsa diğer alanları da ekleyin
    }
}
