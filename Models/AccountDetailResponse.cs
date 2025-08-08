using System.Text.Json.Serialization;

namespace Staj_Proje_1.Models
{
    public class AccountDetailResponse
    {
        [JsonPropertyName("Data")]
        public AccountDetailData Data { get; set; } = new();
    }

    public class AccountDetailData : AccountInfo
    {
        // Eğer detayda farklı ek alanlar varsa burada tanımla.
        // Yoksa AccountInfo’dan miras alarak yetebilir.
    }
}
