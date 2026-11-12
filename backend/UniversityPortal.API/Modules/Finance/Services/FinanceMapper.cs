using UniversityPortal.API.Integrations.BusinessCentral.DTOs;
using UniversityPortal.API.Integrations.BusinessCentral.Exceptions;
using UniversityPortal.API.Modules.Finance.DTOs;

namespace UniversityPortal.API.Modules.Finance.Services;

internal static class FinanceMapper
{
    public static InvoiceSummaryDto ToSummary(BcSalesInvoiceDto bc, DateOnly today) => new(
        InvoiceNumber: bc.Number,
        InvoiceDate: bc.InvoiceDate,
        DueDate: bc.DueDate,
        CurrencyCode: bc.CurrencyCode,
        TotalExcludingTax: bc.TotalAmountExcludingTax,
        TaxAmount: bc.TotalTaxAmount,
        TotalAmount: bc.TotalAmountIncludingTax,
        AmountPaid: bc.TotalAmountIncludingTax - bc.RemainingAmount,
        AmountRemaining: bc.RemainingAmount,
        PaymentStatus: ComputeStatus(bc.TotalAmountIncludingTax, bc.RemainingAmount, bc.DueDate, today));

    public static PaymentStatus ComputeStatus(
        decimal total,
        decimal remaining,
        DateOnly dueDate,
        DateOnly today)
    {
        if (remaining <= 0)
            return PaymentStatus.Paid;

        if (dueDate < today)
            return PaymentStatus.Overdue;

        return remaining < total ? PaymentStatus.PartiallyPaid : PaymentStatus.Unpaid;
    }

    public static string ToBcStatus(InvoiceStatus status) => status switch
    {
        InvoiceStatus.Draft => "Draft",
        InvoiceStatus.InReview => "In Review",
        InvoiceStatus.Open => "Open",
        InvoiceStatus.Paid => "Paid",
        InvoiceStatus.Corrective => "Corrective",
        InvoiceStatus.Canceled => "Canceled",
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };

    public static InvoiceStatus ParseStatus(string bcStatus) => bcStatus switch
    {
        "Draft" => InvoiceStatus.Draft,
        "In Review" => InvoiceStatus.InReview,
        "Open" => InvoiceStatus.Open,
        "Paid" => InvoiceStatus.Paid,
        "Corrective" => InvoiceStatus.Corrective,
        "Canceled" => InvoiceStatus.Canceled,
        _ => throw new BcIntegrationException(
            BcFailureKind.InvalidResponse,
            $"Unknown BC invoice status '{bcStatus}'")
    };

    public static OfficerInvoiceDto ToOfficerInvoice(BcSalesInvoiceDto bc, DateOnly today)
    {
        var status = ParseStatus(bc.Status);
        var posted = status is InvoiceStatus.Open or InvoiceStatus.Paid or InvoiceStatus.Corrective;

        return new OfficerInvoiceDto(
            InvoiceNumber: bc.Number,
            StudentNumber: bc.CustomerNumber,
            StudentName: bc.CustomerName,
            InvoiceDate: bc.InvoiceDate,
            DueDate: bc.DueDate,
            CurrencyCode: bc.CurrencyCode,
            TotalAmount: bc.TotalAmountIncludingTax,
            AmountRemaining: bc.RemainingAmount,
            Status: status,
            PaymentStatus: posted
                ? ComputeStatus(bc.TotalAmountIncludingTax, bc.RemainingAmount, bc.DueDate, today)
                : null);
    }

    public static InvoiceLineDto? ToLine(BcSalesInvoiceLineDto bc) =>
        bc.LineType.Equals("Comment", StringComparison.OrdinalIgnoreCase)
            ? null
            : new InvoiceLineDto(
                Description: bc.Description,
                Quantity: bc.Quantity,
                UnitPrice: bc.UnitPrice,
                DiscountAmount: bc.DiscountAmount,
                AmountExcludingTax: bc.NetAmount,
                TaxAmount: bc.NetAmountIncludingTax - bc.NetAmount,
                AmountIncludingTax: bc.NetAmountIncludingTax);
}
