using System.ComponentModel.DataAnnotations;

namespace UniversityPortal.API.Modules.Administration.DTOs;

public class LoginRequest
{
    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(128)]
    public string Password { get; set; } = string.Empty;
}

public record CurrentUserDto(
    Guid UserId,
    string FullName,
    string Email,
    IReadOnlyList<string> Roles,
    string? StudentNumber);

public record LoginResponse(
    string AccessToken,
    string TokenType,
    DateTime ExpiresAtUtc,
    CurrentUserDto User);
