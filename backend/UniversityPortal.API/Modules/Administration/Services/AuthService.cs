using Microsoft.AspNetCore.Identity;
using UniversityPortal.API.Common.Exceptions;
using UniversityPortal.API.Modules.Administration.DTOs;
using UniversityPortal.API.Modules.Administration.Models;

namespace UniversityPortal.API.Modules.Administration.Services;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
}

public class AuthService(
    UserManager<PortalUser> users,
    ITokenService tokens,
    ILogger<AuthService> logger) : IAuthService
{
    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var user = await users.FindByEmailAsync(request.Email);

        if (user is null || !user.IsActive)
        {
            logger.LogWarning("Sign-in rejected: unknown or disabled account");
            throw InvalidCredentials();
        }

        if (await users.IsLockedOutAsync(user))
        {
            logger.LogWarning("Sign-in rejected: account {UserId} is locked out", user.Id);
            throw InvalidCredentials();
        }

        if (!await users.CheckPasswordAsync(user, request.Password))
        {
            EnsureSucceeded(await users.AccessFailedAsync(user), "Could not record the failed sign-in attempt.");
            logger.LogWarning("Sign-in rejected: wrong password for account {UserId}", user.Id);
            throw InvalidCredentials();
        }

        EnsureSucceeded(
            await users.ResetAccessFailedCountAsync(user),
            "Could not reset the failed sign-in count.");

        var roles = (await users.GetRolesAsync(user)).ToList();
        var token = tokens.CreateAccessToken(user, roles);

        return new LoginResponse(
            token.Value,
            "Bearer",
            token.ExpiresAtUtc,
            new CurrentUserDto(user.Id, user.FullName, user.Email ?? string.Empty, roles, user.StudentNumber));
    }

    private static PortalException InvalidCredentials() =>
        new(401, "Sign-in failed", "Invalid email or password.");

    private static void EnsureSucceeded(IdentityResult result, string message)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException(message + " " +
                string.Join("; ", result.Errors.Select(error => error.Description)));
    }
}
