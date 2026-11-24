namespace UniversityPortal.API.Integrations.BusinessCentral.DTOs;

public class BcJournalDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string BalancingAccountNumber { get; set; } = string.Empty;
}

public class BcPaymentLineDto
{
    public Guid Id { get; set; }
    public int LineNumber { get; set; }
    public string CustomerNumber { get; set; } = string.Empty;
    public DateOnly PostingDate { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public string ExternalDocumentNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string AppliesToInvoiceNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public record BcPaymentLineRequest(
    string CustomerNumber,
    DateOnly PostingDate,
    decimal ReceivedAmount,
    string? AppliesToInvoiceNumber,
    string ExternalDocumentNumber,
    string Description);
