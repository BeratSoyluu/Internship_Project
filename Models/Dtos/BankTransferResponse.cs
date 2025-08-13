using System.Text.Json;
using System.Text.Json.Serialization;

namespace Staj_Proje_1.Models.Dtos
{
    public class BankTransferResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("reference")]
        public string? Reference { get; set; }

        [JsonPropertyName("statusCode")]
        public string? StatusCode { get; set; }

        [JsonPropertyName("statusDescription")]
        public string? StatusDescription { get; set; }

        [JsonPropertyName("rawBody")]
        public string RawBody { get; set; } = "";
    }
}
