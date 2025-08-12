namespace Staj_Proje_1.Models
{
    public class Transaction
    {
        public ulong Id { get; set; }
        public ulong AccountId { get; set; }
        public DateTime TransactionDate { get; set; }
        public string? Description { get; set; }
        public decimal Amount { get; set; }
        public decimal? Balance { get; set; }
        public string? TransactionCode { get; set; }
        public string? TransactionName { get; set; }
        public string CurrencyCode { get; set; } = "TL";
        public int TransactionType { get; set; }
        public string TransactionId { get; set; } = string.Empty;

        public Account? Account { get; set; }
    }
}