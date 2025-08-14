namespace Staj_Proje_1.Models.Dtos
{
    public class AuthResponseDto
    {
        public string Token { get; set; } = "";
        public DateTime ExpiresAtUtc { get; set; }
        public string Email { get; set; } = "";
        public string? UserId { get; set; }
        public string[] Roles { get; set; } = Array.Empty<string>();
    }
}
