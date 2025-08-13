namespace Staj_Proje_1.Models
{
    public class AccountTransactionsRequest
    {
        public string AccountNumber { get; set; } = "";

        // Gün-Ay-Yıl formatında tarih ("dd-MM-yyyy")
        public string StartDate { get; set; } = ""; 
        public string EndDate { get; set; } = "";
    }
}
