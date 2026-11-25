namespace UniversityPortal.API.Modules.Finance.Models;

public class ReceiptIssue
{
    public long Id { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public string StudentNumber { get; set; } = string.Empty;
    public int CopyNumber { get; set; }
    public Guid IssuedByUserId { get; set; }
    public DateTime IssuedAtUtc { get; set; }
}
