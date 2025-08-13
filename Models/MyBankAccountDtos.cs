using System.ComponentModel.DataAnnotations;

namespace Staj_Proje_1.Models.Dtos
{
    public class MyBankAccountCreateDto
    {
        [Required, StringLength(34)]
        [RegularExpression(@"^[A-Z]{2}[0-9A-Z]{13,32}$", ErrorMessage = "Geçersiz IBAN formatı")]
        public string Iban { get; set; } = string.Empty;

        [Required, StringLength(20)]
        public string AccountNumber { get; set; } = string.Empty;

        [Required, StringLength(20)]
        [RegularExpression(@"^\+?\d{10,15}$", ErrorMessage = "Telefon +905xxxxxxxxx formatında olmalı")]
        public string PhoneNumber { get; set; } = string.Empty;

        [StringLength(100)]
        public string? AccountName { get; set; }

        [StringLength(3)]
        public string? Currency { get; set; } = "TRY";
    }

    public class MyBankAccountUpdateDto
    {
        [StringLength(20)]
        [RegularExpression(@"^\+?\d{10,15}$", ErrorMessage = "Telefon +905xxxxxxxxx formatında olmalı")]
        public string? PhoneNumber { get; set; }

        [StringLength(100)]
        public string? AccountName { get; set; }
    }

    public class MyBankAccountDto
    {
        public int Id { get; set; }
        public string Iban { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? AccountName { get; set; }
        public string? Currency { get; set; }
        public string? BankName { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
