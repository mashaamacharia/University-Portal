namespace UniversityPortal.API.Modules.Finance.Models;

public enum PaymentRequestStatus
{
    Pending,
    Posted,
    Failed,
    Unknown
}

public class PaymentRequest
{
    public Guid Id { get; set; }
    public string StudentNumber { get; set; } = string.Empty;
    public string? InvoiceNumber { get; set; }
    public decimal Amount { get; set; }
    public DateOnly PaymentDate { get; set; }
    public string Method { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public string PayloadHash { get; set; } = string.Empty;
    public PaymentRequestStatus Status { get; set; }
    public string? ReceiptNumber { get; set; }
    public int? LedgerEntryNumber { get; set; }
    public string? FailureReason { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}
