using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniversityPortal.API.Infrastructure.Persistence;

namespace UniversityPortal.API.Common.Health;

[ApiController]
[Route("api/health")]
[AllowAnonymous]
public class HealthController : ControllerBase
{
    private readonly PortalDbContext _db;

    public HealthController(PortalDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var databaseUp = await _db.Database.CanConnectAsync(ct);

        var body = new
        {
            status = databaseUp ? "Healthy" : "Degraded",
            database = databaseUp ? "Up" : "Down",
            timestampUtc = DateTime.UtcNow
        };

        return databaseUp ? Ok(body) : StatusCode(StatusCodes.Status503ServiceUnavailable, body);
    }
}