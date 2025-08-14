using System.ComponentModel.DataAnnotations;

namespace Staj_Proje_1.Models.Dtos
{
    public sealed class LoginDto
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}
}
