using System.ComponentModel.DataAnnotations;

namespace YourNamespace.Models
{
    /// <summary>
    /// Hesap detayı isteği için sadece hesap numarası gönderir.
    /// </summary>
    public class AccountDetailRequest
    {
        [Required]
        public string AccountNumber { get; set; }
    }
}