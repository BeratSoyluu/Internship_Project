using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Staj_Proje_1.Models;

namespace Staj_Proje_1.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<ApplicationUser>   Kullanicilar       { get; set; } = null!;
        public DbSet<MyBankAccount>     MyBankAccounts     { get; set; } = null!;
        public DbSet<MyBankTransaction> MyBankTransactions { get; set; } = null!;
        public DbSet<MyBankTransfer>    MyBankTransfers    { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ---------------- ApplicationUser ----------------
            builder.Entity<ApplicationUser>(entity =>
            {
                entity.ToTable("AspNetUsers");

                entity.Property(e => e.Bakiye)
                      .HasPrecision(18, 2)
                      .HasDefaultValue(0.00m);

                entity.Property(e => e.KullaniciAdi)
                      .HasMaxLength(50);

                entity.HasIndex(e => e.KullaniciAdi)
                      .IsUnique();
            });

            // ---------------- MyBankAccount ----------------
            builder.Entity<MyBankAccount>(e =>
            {
                e.ToTable("MyBankAccounts");
                e.HasKey(x => x.Id);

                e.Property(x => x.Iban).IsRequired().HasMaxLength(34);
                e.Property(x => x.AccountNumber).IsRequired().HasMaxLength(20);
                e.Property(x => x.PhoneNumber).IsRequired().HasMaxLength(20);
                e.Property(x => x.AccountName).HasMaxLength(100);
                e.Property(x => x.Currency).IsRequired().HasMaxLength(3);

                e.HasIndex(x => x.Iban).IsUnique();
                e.HasIndex(x => x.AccountNumber);

                // 🔹 İLİŞKİ: hesap sahibi kullanıcı
                e.HasOne(x => x.OwnerUser)
                 .WithMany() // istersen ApplicationUser içine ICollection<MyBankAccount> Accounts ekleyip .WithMany(u=>u.Accounts) yapabilirsin
                 .HasForeignKey(x => x.OwnerUserId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ---------------- MyBankTransaction ----------------
            builder.Entity<MyBankTransaction>(e =>
            {
                e.ToTable("MyBankTransactions");
                e.HasKey(x => x.Id);

                e.Property(x => x.Currency).IsRequired().HasMaxLength(3);
                e.Property(x => x.Description).HasMaxLength(256);
                e.Property(x => x.Direction).HasMaxLength(16);
                e.Property(x => x.ExternalId).HasMaxLength(64);

                e.Property(x => x.Amount).HasPrecision(18, 2);
                e.Property(x => x.BalanceAfter).HasPrecision(18, 2);

                e.HasIndex(x => new { x.MyBankAccountId, x.TransactionDate });
                e.HasIndex(x => new { x.MyBankAccountId, x.ExternalId });

                e.HasOne(x => x.Account)
                 .WithMany()
                 .HasForeignKey(x => x.MyBankAccountId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ---------------- MyBankTransfer ----------------
            builder.Entity<MyBankTransfer>(e =>
            {
                e.ToTable("MyBankTransfers");
                e.HasKey(x => x.Id);

                e.Property(x => x.ToIban).IsRequired().HasMaxLength(34);
                e.Property(x => x.ToName).IsRequired().HasMaxLength(120);
                e.Property(x => x.Description).HasMaxLength(240);
                e.Property(x => x.Currency).IsRequired().HasMaxLength(3);
                e.Property(x => x.Amount).HasPrecision(18, 2);
                e.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);

                e.HasIndex(x => x.FromAccountId);
                e.HasIndex(x => x.RequestedAt);
                e.HasIndex(x => x.BankReference);

                e.HasOne(x => x.FromAccount)
                 .WithMany()
                 .HasForeignKey(x => x.FromAccountId)
                 .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
