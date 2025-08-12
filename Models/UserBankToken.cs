// Models/UserBankToken.cs
public class UserBankToken
{
    public int Id { get; set; }
    public string UserId { get; set; } = null!;
    public string AccessToken { get; set; } = null!;
    public DateTime ExpiresAtUtc { get; set; }
    public string? ConsentId { get; set; }     // gerekiyorsa
    public string? RefreshToken { get; set; }  // varsa
}
