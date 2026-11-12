namespace UniversityPortal.API.Common.Security;

public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    string? StudentNumber { get; }

    bool IsInRole(string role);
}
