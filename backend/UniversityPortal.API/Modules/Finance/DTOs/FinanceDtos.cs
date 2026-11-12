namespace UniversityPortal.API.Modules.Finance.DTOs;

public enum PaymentStatus
{
    Unpaid,
    PartiallyPaid,
    Paid,
    Overdue
}

public record StudentInfoDto(string StudentNumber, string Name, string Email);

public record BalanceSummaryDto(string CurrencyCode, decimal Balance, decimal OverdueAmount);

public record InvoiceSummaryDto(
    string InvoiceNumber,
    DateOnly InvoiceDate,
    DateOnly DueDate,
    string CurrencyCode,
    decimal TotalExcludingTax,
    decimal TaxAmount,
    decimal TotalAmount,
    decimal AmountPaid,
    decimal AmountRemaining,
    PaymentStatus PaymentStatus);

public record InvoiceLineDto(
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountAmount,
    decimal AmountExcludingTax,
    decimal TaxAmount,
    decimal AmountIncludingTax);

public record InvoiceDetailDto(InvoiceSummaryDto Invoice, IReadOnlyList<InvoiceLineDto> Lines);

public record FeeStatementDto(
    StudentInfoDto Student,
    BalanceSummaryDto Balance,
    IReadOnlyList<InvoiceSummaryDto> Invoices,
    DateTime GeneratedAtUtc);
