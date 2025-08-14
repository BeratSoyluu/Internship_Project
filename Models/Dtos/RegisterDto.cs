namespace Staj_Proje_1.Models.Dtos
{
    public sealed class RegisterDto
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";

        public string? Phone { get; set; }
        public string? FirstName { get; set; }
        public string? LastName  { get; set; }

        // Tek bakiye alanı (opsiyonel)
        public decimal? Bakiye { get; set; }
    }
}
