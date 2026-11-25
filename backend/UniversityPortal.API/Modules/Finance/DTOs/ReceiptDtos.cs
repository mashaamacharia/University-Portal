namespace UniversityPortal.API.Modules.Finance.DTOs;

public enum ReceiptStatus
{
    Valid,
    Reversed
}

public record ReceiptIssuerDto(
    string Name,
    IReadOnlyList<string> AddressLines,
    string? Phone,
    string? Email,
    string? FooterNote);

public record ReceiptInvoiceBalanceDto(
    string InvoiceNumber,
    decimal RemainingBalance,
    string CurrencyCode);

public record ReceiptDto(
    string ReceiptNumber,
    DateOnly PaymentDate,
    ReceiptStatus Status,
    ReceiptIssuerDto Issuer,
    StudentInfoDto Student,
    decimal AmountReceived,
    string CurrencyCode,
    decimal? CurrentAccountBalance,
    string AccountBalanceCurrencyCode,
    IReadOnlyList<ReceiptInvoiceBalanceDto> InvoiceBalances,
    string? Method,
    string? Reference,
    string? ReceivedBy,
    IReadOnlyList<string> SettledInvoices,
    int PrintCount,
    int? CopyNumber);
