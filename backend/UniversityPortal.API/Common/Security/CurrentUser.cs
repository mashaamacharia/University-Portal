namespace UniversityPortal.API.Common.Security;

public sealed class DevCurrentUser : ICurrentUser
{
    public DevCurrentUser(IConfiguration configuration)
    {
        StudentNumber = configuration["DevIdentity:StudentNumber"];
    }

    public string? StudentNumber { get; }
}

public sealed class AnonymousCurrentUser : ICurrentUser
{
    public string? StudentNumber => null;
}
