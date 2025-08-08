namespace Staj_Proje_1.Models
{
    public class RegisterDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string KullaniciAdi { get; set; } = string.Empty;
        public decimal Bakiye { get; set; } = 0;
    }
}
