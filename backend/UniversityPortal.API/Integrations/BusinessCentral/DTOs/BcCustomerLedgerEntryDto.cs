using System.Text.Json.Serialization;

namespace UniversityPortal.API.Integrations.BusinessCentral.DTOs;

public class BcCustomerLedgerEntryDto
{
    public Guid Id { get; set; }
    public int EntryNumber { get; set; }
    public string CustomerNumber { get; set; } = string.Empty;
    public DateOnly PostingDate { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string DocumentNumber { get; set; } = string.Empty;
    public string ExternalDocumentNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateOnly DueDate { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal RemainingAmount { get; set; }
    public bool Open { get; set; }
    public DateOnly ClosedAtDate { get; set; }
    public int ClosedByEntryNumber { get; set; }
    public bool Reversed { get; set; }
    public DateTime LastModifiedDateTime { get; set; }

    [JsonIgnore]
    public string DocumentTypeName => DocumentType.Replace("_x0020_", " ");
}
