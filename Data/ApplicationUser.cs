using Microsoft.AspNetCore.Identity;

namespace Staj_Proje_1.Data
{
    public class ApplicationUser : IdentityUser
    {
        public string KullaniciAdi { get; set; } = string.Empty;
        public decimal Bakiye { get; set; } = 0;
    }
}
