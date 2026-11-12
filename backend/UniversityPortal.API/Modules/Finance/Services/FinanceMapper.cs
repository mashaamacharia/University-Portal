using UniversityPortal.API.Integrations.BusinessCentral.DTOs;
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
