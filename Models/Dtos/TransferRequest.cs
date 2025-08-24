public sealed class TransferRequest
{
    public string FromIban { get; set; } = "";
    public string ToIban   { get; set; } = "";
    public decimal Amount  { get; set; }
    public string? Description { get; set; }

    // ✅ Alıcı adı formdan gelecek (ör. "Ali Veli")
    public string ReceiverName { get; set; } = "";
}
