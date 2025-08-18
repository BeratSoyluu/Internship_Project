namespace Staj_Proje_1.Models.OpenBanking;

public enum BankCode { vakif, mybank }

public record BankDto(
    string Id,
    string Name,
    BankCode Code,
    decimal BalanceTRY,
    bool Connected
);

public record AccountDto(
    string Id,
    BankCode BankCode,
    string Name,
    string IbanMasked,
    decimal Balance,
    string Currency // "TRY"
);

public record TransactionDto(
    string Id,
    BankCode BankCode,
    DateTime Date,
    string Description,
    decimal Amount,
    string Currency
);

public record LinkBankRequest(BankCode BankCode);
