namespace UniversityPortal.API.Common.Auditing;

public interface IAuditLog
{
    Task WriteAsync(
        string action,
        string entityType,
        string? entityId,
        bool success,
        object? details = null,
        CancellationToken ct = default);
}
