using System.Text.Json.Serialization;

public class AccountDetailApiRequest
{


    [JsonPropertyName("Body")]
    public BodyModel Body { get; set; }
   

    public class BodyModel
    {
        [JsonPropertyName("accountNumber")] // portal küçük harfle yazıyorsa burada da küçük olsun!
        public string AccountNumber { get; set; }
    }
}
