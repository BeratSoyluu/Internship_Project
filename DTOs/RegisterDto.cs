using System.ComponentModel.DataAnnotations;

namespace Staj_Proje_1.Models.Dtos
{
    public class RegisterDto
    {
        [Required]
        public string FullName { get; set; } = "";

        [Required]
        public string Phone { get; set; } = "";

        [Required, EmailAddress]
        public string Email { get; set; } = "";

        [Required, MinLength(6)]
        public string Password { get; set; } = "";
    }
}
