
using Staj_Proje_1.Models; // ApplicationUser için

namespace Staj_Proje_1.Models.Entities
{
    public class Account
    {
        public ulong Id { get; set; }

        // Identity default key string olduğu için FK string tutmak en rahatı:
        public string UserId { get; set; } = string.Empty;

        public string AccountNumber { get; set; } = string.Empty;
        public string? Iban { get; set; }
        public string Currency { get; set; } = "TL";
        public decimal Balance { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ApplicationUser? User { get; set; }  // <— tip düzeltildi
        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}
