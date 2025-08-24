using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Staj_Proje_1.Models
{
    public class MyBankTransaction
    {
        [Key]
        public long Id { get; set; }

        // ----------------- İlişki / FK -----------------
        [Required]
        public int MyBankAccountId { get; set; }

        [ForeignKey(nameof(MyBankAccountId))]
        public MyBankAccount Account { get; set; } = null!;

        // ----------------- İçerik -----------------
        [Required]
        public DateTime TransactionDate { get; set; } // bankadan gelen tarih (UTC önerilir)

        [Required, StringLength(3)]
        public string Currency { get; set; } = "TRY";

        [Required, Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }           // + gelir, - gider

        [StringLength(16)]
        public string? Direction { get; set; }        // "CR" / "DB" (opsiyonel)

        [StringLength(256)]
        public string? Description { get; set; }

        [StringLength(64)]
        public string? ExternalId { get; set; }       // bankanın işlem id'si (varsa)

        [Column(TypeName = "decimal(18,2)")]
        public decimal? BalanceAfter { get; set; }    // opsiyonel

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // sisteme kaydedilme
    }
}
