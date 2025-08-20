// Models/Dtos/Vakif/VakifAccountRowDto.cs
namespace Staj_Proje_1.Models.Dtos.Vakif;

public record VakifAccountRowDto(
    string Currency,
    DateTime? LastTransactionDate,
    string Status,
    string Iban,
    decimal Balance,
    string AccountType,
    string AccountNumber
);
