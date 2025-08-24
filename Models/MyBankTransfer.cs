using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Staj_Proje_1.Models;

namespace Staj_Proje_1.Models
{
    public enum TransferStatus { Pending, Completed, Failed }

    public class MyBankTransfer
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public int FromAccountId { get; set; }

        [ForeignKey(nameof(FromAccountId))]
        public MyBankAccount FromAccount { get; set; } = null!;

        [Required, StringLength(34)]
        public string ToIban { get; set; } = string.Empty;

        [Required, StringLength(120)]
        public string ToName { get; set; } = string.Empty;

        [Required, Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required, StringLength(3)]
        public string Currency { get; set; } = "TRY";

        [StringLength(240)]
        public string? Description { get; set; }

        [Required]
        public TransferStatus Status { get; set; } = TransferStatus.Pending;

        [StringLength(64)]
        public string? BankReference { get; set; }

        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
    }
}
