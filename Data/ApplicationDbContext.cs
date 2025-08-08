using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

// Bu kod, kullanıcı yönetimi (Identity) tablolarını EF Core üzerinden kullanmanızı; ApplicationUser varlığınıza özel ayarları (örneğin bakiye için ondalık hassasiyet) da eklemenizi sağlayan “veritabanı bağlamı” (DbContext) tanımıdır.

namespace Staj_Proje_1.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // İsteğe bağlı: Kullanıcı tablosu ile Kullanicilar DbSet'i
        public DbSet<ApplicationUser> Kullanicilar { get; set; }

        // Model konfigürasyonları
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Bakiye alanı için decimal precision (18,2) ayarı
            builder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(e => e.Bakiye)
                      .HasPrecision(18, 2);
            });
        }
    }
}
