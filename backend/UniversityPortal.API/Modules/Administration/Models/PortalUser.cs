using Microsoft.AspNetCore.Identity;

namespace UniversityPortal.API.Modules.Administration.Models;

public class PortalUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;

    public string? StudentNumber { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; }
}
