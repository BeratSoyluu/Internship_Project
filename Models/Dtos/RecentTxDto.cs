// Models/Dtos/RecentTxDto.cs
namespace Staj_Proje_1.Models.Dtos
{
    public record RecentTxDto(
        long Id,
        int AccountId,
        string AccountName,
        DateTime TransactionDate,
        string? Description,
        string Direction,         // "IN" | "OUT"
        decimal Amount,
        decimal? BalanceAfter,
        string Currency
    );
}
