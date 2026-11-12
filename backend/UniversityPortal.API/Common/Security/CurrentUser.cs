namespace UniversityPortal.API.Common.Security;

public sealed class DevCurrentUser : ICurrentUser
{
    private readonly HashSet<string> _roles;

    public DevCurrentUser(IConfiguration configuration)
    {
        StudentNumber = configuration["DevIdentity:StudentNumber"];
        _roles = (configuration.GetSection("DevIdentity:Roles").Get<string[]>() ?? Array.Empty<string>())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public string? StudentNumber { get; }
    public bool IsAuthenticated => StudentNumber is not null || _roles.Count > 0;
    public bool IsInRole(string role) => _roles.Contains(role);
}

public sealed class AnonymousCurrentUser : ICurrentUser
{
    public bool IsAuthenticated => false;
    public string? StudentNumber => null;
    public bool IsInRole(string role) => false;
}
