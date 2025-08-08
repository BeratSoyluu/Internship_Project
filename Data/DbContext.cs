using Microsoft.EntityFrameworkCore;
// yaptığı şey aslında veritabanı ile .NET modeliniz arasındaki “eşleştirmeyi” tanımlamak


// Siz kendi uygulamanız için bir “context” türettiğinizde, EF Core’a hangi tabloları yöneteceğinizi, nasıl bağlanacağınızı vb. öğretmiş olursunuz.
public class MyDbContext : DbContext
{
    public MyDbContext(DbContextOptions<MyDbContext> options)
        : base(options)
    {
    }

    public DbSet<Kullanici> Kullanicilar { get; set; } // Örnek tablo
}

public class Kullanici
{
    public int Id { get; set; }
    public string Ad { get; set; }
}
