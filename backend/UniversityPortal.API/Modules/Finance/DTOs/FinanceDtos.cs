using System.ComponentModel.DataAnnotations;

namespace UniversityPortal.API.Modules.Finance.DTOs;

public enum PaymentStatus
{
    Unpaid,
    PartiallyPaid,
    Paid,
    Overdue
}

public enum InvoiceStatus
{
    Draft,
    InReview,
    Open,
    Paid,
    Corrective,
    Canceled
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

public record AgingBucketDto(string Label, decimal Amount);

public record OfficerDashboardDto(
    DateOnly AsOfDate,
    decimal TotalOutstanding,
    IReadOnlyList<AgingBucketDto> Aging,
    int StudentsWithBalance,
    int StudentsWithOverdue,
    int DraftInvoices,
    int InvoicesInReview,
    DateTime GeneratedAtUtc);

public record OutstandingBalanceDto(
    string StudentNumber,
    string StudentName,
    string Email,
    string PhoneNumber,
    decimal Balance,
    decimal OverdueAmount);

public record OfficerInvoiceDto(
    string InvoiceNumber,
    string StudentNumber,
    string StudentName,
    DateOnly InvoiceDate,
    DateOnly DueDate,
    string CurrencyCode,
    decimal TotalAmount,
    decimal AmountRemaining,
    InvoiceStatus Status,
    PaymentStatus? PaymentStatus);

public class OfficerInvoiceQuery : IValidatableObject
{
    public InvoiceStatus? Status { get; set; }

    [StringLength(20)]
    public string? CustomerNumber { get; set; }

    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }

    [Range(1, 10000)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 25;

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (From is { } from && To is { } to && from > to)
            yield return new ValidationResult("'From' must not be after 'To'.", new[] { nameof(From), nameof(To) });
    }
}

public class OutstandingBalanceQuery
{
    [Range(typeof(decimal), "0", "1000000000")]
    public decimal MinBalance { get; set; } = 0;

    public bool OverdueOnly { get; set; }

    [Range(1, 10000)]
    public int Page { get; set; } = 1;

    [Range(1, 50)]
    public int PageSize { get; set; } = 25;
}
