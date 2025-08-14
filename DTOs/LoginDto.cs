using System.ComponentModel.DataAnnotations;

namespace Staj_Proje_1.Models.Dtos
{
    public class LoginDto
    {
        [Required, EmailAddress]
        public string Email { get; set; } = "";

        [Required]
        public string Password { get; set; } = "";
    }
}
