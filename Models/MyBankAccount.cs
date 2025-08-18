using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Staj_Proje_1.Models
{
    public class MyBankAccount
    {
        public int Id { get; set; }

        [MaxLength(34)]
        public string Iban { get; set; } = string.Empty;

        [MaxLength(32)]
        public string AccountNumber { get; set; } = string.Empty;

        [MaxLength(100)]
        public string AccountName { get; set; } = string.Empty;

        [MaxLength(3)]
        public string Currency { get; set; } = "TRY";

        [MaxLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [MaxLength(64)]
        public string BankName { get; set; } = "MyBank";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Kullanıcı ile ilişki
        [Required]
        public string OwnerUserId { get; set; } = default!;   // null değil

        public ApplicationUser? OwnerUser { get; set; }

        // ÖNEMLİ: object değil, decimal (veya decimal?)
        [Column(TypeName = "decimal(18,2)")]
        public decimal Balance { get; set; } = 0m;
    }
}
