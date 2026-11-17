namespace UniversityPortal.API.Common.Auditing;

public class AuditEntry
{
    public Guid Id { get; set; }

    public DateTime OccurredAtUtc { get; set; }

    public Guid? ActorUserId { get; set; }

    public string Action { get; set; } = string.Empty;

    public string EntityType { get; set; } = string.Empty;

    public string? EntityId { get; set; }

    public string Outcome { get; set; } = string.Empty;

    public string? Details { get; set; }

    public string? TraceId { get; set; }
}
