using Staj_Proje_1.Models;

public class MyBankAccount
{
    public int Id { get; set; }

    public string Iban { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string Currency { get; set; } = "TRY";
    public string PhoneNumber { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // 🔹 Eksik olan alanlar
    public string BankName { get; set; } = string.Empty; // Banka adı (örn: MyBank, Vakıfbank)
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow; // Son güncelleme zamanı

    // Kullanıcı ile ilişki
    public string? OwnerUserId { get; set; }  // ? yaptık
    public ApplicationUser? OwnerUser { get; set; }

}
