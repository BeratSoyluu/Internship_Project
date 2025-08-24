using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Staj_Proje_1.Models
{
    /// <summary>
    /// MyBank birleşik hareket kaydı:
    ///  - Direction: "IN" (giriş) / "OUT" (çıkış)
    ///  - Amount: daima pozitif, yönü Direction belirler
    /// </summary>
    public class MyBankTransaction
    {
        [Key]
        public long Id { get; set; }

        // ----------------- İlişki / FK -----------------
        [Required]
        public int MyBankAccountId { get; set; }

        [ForeignKey(nameof(MyBankAccountId))]
        public MyBankAccount Account { get; set; } = null!;

        // Kart/listede gösterim için hesap adı (snapshot)
        [Required, StringLength(120)]
        public string AccountName { get; set; } = string.Empty;

        // ----------------- İçerik -----------------
        [Required]
        public DateTime TransactionDate { get; set; } // UTC önerilir

        [Required, StringLength(3)]
        public string Currency { get; set; } = "TRY";

        /// <summary>
        /// Pozitif tutar. Giriş/çıkış Direction ile belirlenir.
        /// </summary>
        [Required, Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        /// <summary>
        /// "IN" (giriş) veya "OUT" (çıkış)
        /// </summary>
        [Required, StringLength(4)]
        public string Direction { get; set; } = "OUT";

        [StringLength(256)]
        public string? Description { get; set; }

        // Bankanın işlem/id referansı varsa
        [StringLength(64)]
        public string? ExternalId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? BalanceAfter { get; set; }    // opsiyonel (snapshot)

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
