namespace UniversityPortal.API.Integrations.BusinessCentral.DTOs;

public class BcSalesInvoiceLineDto
{
    public Guid Id { get; set; }

    public Guid DocumentId { get; set; }

    public int Sequence { get; set; }

    public string LineType { get; set; } = string.Empty;

    public string LineObjectNumber { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Description2 { get; set; } = string.Empty;

    public string UnitOfMeasureCode { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal DiscountPercent { get; set; }

    public decimal AmountExcludingTax { get; set; }

    public string TaxCode { get; set; } = string.Empty;

    public decimal TaxPercent { get; set; }

    public decimal TotalTaxAmount { get; set; }

    public decimal AmountIncludingTax { get; set; }

    public decimal NetAmount { get; set; }

    public decimal NetAmountIncludingTax { get; set; }
}
