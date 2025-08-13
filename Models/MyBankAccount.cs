using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Staj_Proje_1.Models
{
    public class MyBankAccount
    {
        [Key]
        public int Id { get; set; }

        // Uygulama kullanıcısına bağlamak istersen (opsiyonel)
        public string? OwnerUserId { get; set; }

        [Required, StringLength(34)]               // IBAN max 34
        public string Iban { get; set; } = string.Empty;

        [Required, StringLength(20)]
        public string AccountNumber { get; set; } = string.Empty;

        [Required, StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty; // E.164 önerilir (+9055…)

        [StringLength(100)]
        public string? BankName { get; set; } = "MyBank";

        [StringLength(100)]
        public string? AccountName { get; set; } = null; // “Vadesiz TRY” gibi

        [StringLength(3)]
        public string? Currency { get; set; } = "TRY";

        // Audit
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
