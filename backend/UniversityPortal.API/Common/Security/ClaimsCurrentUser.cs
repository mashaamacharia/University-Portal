using UniversityPortal.API.Common.Constants;

namespace UniversityPortal.API.Common.Security;

public class ClaimsCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private readonly System.Security.Claims.ClaimsPrincipal? _principal = accessor.HttpContext?.User;

    public bool IsAuthenticated => _principal?.Identity?.IsAuthenticated == true;

    public string? StudentNumber =>
        IsAuthenticated ? _principal!.FindFirst(ClaimNames.StudentNumber)?.Value : null;

    public bool IsInRole(string role) => IsAuthenticated && _principal!.IsInRole(role);
}
