namespace UniversityPortal.API.Modules.Finance.Models;

public enum InvoiceRequestStatus
{
    Pending,
    Draft,
    Posted,
    Failed,
    Discarded
}

public class InvoiceRequest
{
    public Guid Id { get; set; }

    public string IdempotencyKey { get; set; } = string.Empty;

    public string RequestHash { get; set; } = string.Empty;

    public string StudentNumber { get; set; } = string.Empty;

    public InvoiceRequestStatus Status { get; set; }

    public Guid? BcInvoiceId { get; set; }

    public string? BcInvoiceNumber { get; set; }

    public string? LastError { get; set; }

    public Guid CreatedByUserId { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public Guid? PostedByUserId { get; set; }

    public DateTime? PostedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public uint Version { get; set; }
}
