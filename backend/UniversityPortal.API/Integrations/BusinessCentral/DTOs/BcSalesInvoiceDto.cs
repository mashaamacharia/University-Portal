namespace UniversityPortal.API.Integrations.BusinessCentral.DTOs;

public class BcSalesInvoiceDto
{
    public Guid Id { get; set; }

    public string Number { get; set; } = string.Empty;

    public string ExternalDocumentNumber { get; set; } = string.Empty;

    public DateOnly InvoiceDate { get; set; }

    public DateOnly PostingDate { get; set; }

    public DateOnly DueDate { get; set; }

    public Guid CustomerId { get; set; }

    public string CustomerNumber { get; set; } = string.Empty;

    public string CustomerName { get; set; } = string.Empty;

    public string CurrencyCode { get; set; } = string.Empty;

    public decimal TotalAmountExcludingTax { get; set; }

    public decimal TotalTaxAmount { get; set; }

    public decimal TotalAmountIncludingTax { get; set; }

    public decimal RemainingAmount { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime LastModifiedDateTime { get; set; }
}
