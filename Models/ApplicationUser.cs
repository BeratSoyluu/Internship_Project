using Microsoft.AspNetCore.Identity;

namespace Staj_Proje_1.Models
{
    // İhtiyacın olan ekstra alanları burada tut
    public class ApplicationUser : IdentityUser
    {
        public string? KullaniciAdi { get; set; }
        public decimal? Bakiye { get; set; }
    }
}
