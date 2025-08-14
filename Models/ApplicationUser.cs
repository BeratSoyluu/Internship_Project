using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Staj_Proje_1.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? KullaniciAdi { get; set; }

        [Precision(18, 2)]
        public decimal? Bakiye { get; set; }

        // İstersen kullan (Auth/MyBank’te referans verirsen):
        public string? FirstName { get; set; }
        public string? LastName  { get; set; }
    }
}
