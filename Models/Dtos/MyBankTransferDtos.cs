using System.ComponentModel.DataAnnotations;

namespace Staj_Proje_1.Models.Dtos
{
    // Sade istek: Yalnızca IBAN + AdSoyad + Tutar
    public class TransferCreateDto
    {
        [Required, StringLength(34)]
        public string ToIban { get; set; } = string.Empty;

        [Required, StringLength(120)]
        public string ToName { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Tutar 0'dan büyük olmalı")]
        public decimal Amount { get; set; }
    }

    public class TransferDto
    {
        public long   Id { get; set; }
        public int    FromAccountId { get; set; }
        public string FromAccountNumber { get; set; } = "";
        public string ToIban { get; set; } = "";
        public string ToName { get; set; } = "";
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "TRY";
        public string? Description { get; set; }
        public string Status { get; set; } = "Pending";
        public string? BankReference { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}
