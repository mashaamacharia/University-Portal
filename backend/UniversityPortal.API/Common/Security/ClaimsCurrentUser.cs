using UniversityPortal.API.Common.Constants;
using Microsoft.IdentityModel.JsonWebTokens;

namespace UniversityPortal.API.Common.Security;

public class ClaimsCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private readonly System.Security.Claims.ClaimsPrincipal? _principal = accessor.HttpContext?.User;

    public bool IsAuthenticated => _principal?.Identity?.IsAuthenticated == true;

    public Guid? UserId =>
        IsAuthenticated &&
        Guid.TryParse(_principal!.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var id)
            ? id
            : null;

    public string? StudentNumber =>
        IsAuthenticated ? _principal!.FindFirst(ClaimNames.StudentNumber)?.Value : null;

    public bool IsInRole(string role) => IsAuthenticated && _principal!.IsInRole(role);
}
