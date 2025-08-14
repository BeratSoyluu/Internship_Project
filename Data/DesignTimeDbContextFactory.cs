using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure; // MySqlServerVersion
using Staj_Proje_1.Data;

namespace Staj_Proje_1.Data
{
    /// <summary>
    /// EF Core CLI (dotnet ef) tasarım zamanında ApplicationDbContext'i nasıl oluşturacağını buradan öğrenir.
    /// AutoDetect yerine sabit ServerVersion kullanıyoruz ki MySQL'e bağlanmadan migrations üretilebilsin.
    /// </summary>
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            // appsettings.json'u yükle (çalışma dizini proje kökü olmalı)
            var cfg = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile($"appsettings.Development.json", optional: true)
                .Build();

            var cs = cfg.GetConnectionString("DefaultConnection")
                   ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection bulunamadı.");

            // Kendi veritabanı sürümüne göre değiştir:
            // MySQL 8.x için:
            var serverVersion = new MySqlServerVersion(new Version(8, 0, 36));
            // MariaDB kullanıyorsan:
            // var serverVersion = new MariaDbServerVersion(new Version(10, 11, 6));

            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseMySql(cs, serverVersion, my =>
            {
                my.EnableRetryOnFailure(5, TimeSpan.FromSeconds(5), null);
            });

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}
