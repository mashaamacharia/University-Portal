using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using UniversityPortal.API.Common.Auditing;
using UniversityPortal.API.Common.Security;

namespace UniversityPortal.API.Infrastructure.Persistence;

public class AuditLog : IAuditLog
{
    private readonly DbContextOptions<PortalDbContext> _options;
    private readonly ICurrentUser _user;
    private readonly IHttpContextAccessor _http;
    private readonly TimeProvider _time;

    public AuditLog(
        DbContextOptions<PortalDbContext> options,
        ICurrentUser user,
        IHttpContextAccessor http,
        TimeProvider time)
    {
        _options = options;
        _user = user;
        _http = http;
        _time = time;
    }

    public async Task WriteAsync(
        string action,
        string entityType,
        string? entityId,
        bool success,
        object? details = null,
        CancellationToken ct = default)
    {
        await using var db = new PortalDbContext(_options);

        db.AuditEntries.Add(new AuditEntry
        {
            Id = Guid.NewGuid(),
            OccurredAtUtc = _time.GetUtcNow().UtcDateTime,
            ActorUserId = _user.UserId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Outcome = success ? "Success" : "Failure",
            Details = details is null ? null : JsonSerializer.Serialize(details),
            TraceId = Activity.Current?.Id ?? _http.HttpContext?.TraceIdentifier
        });

        await db.SaveChangesAsync(ct);
    }
}
