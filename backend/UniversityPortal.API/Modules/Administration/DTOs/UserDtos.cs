using System.ComponentModel.DataAnnotations;

namespace UniversityPortal.API.Modules.Administration.DTOs;

public enum PortalRole
{
    Student,
    FinanceOfficer,
    Administrator
}

public class CreateUserRequest : IValidatableObject
{
    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(200, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [Required, StringLength(128)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public PortalRole? Role { get; set; }

    [StringLength(20)]
    public string? StudentNumber { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Role is { } role && !Enum.IsDefined(role))
            yield return new ValidationResult("The selected role is invalid.", new[] { nameof(Role) });

        var hasNumber = !string.IsNullOrWhiteSpace(StudentNumber);

        if (Role == PortalRole.Student && !hasNumber)
            yield return new ValidationResult(
                "A student account needs a student number.",
                new[] { nameof(StudentNumber) });

        if (Role is not null && Role != PortalRole.Student && hasNumber)
            yield return new ValidationResult(
                "Only student accounts have a student number.",
                new[] { nameof(StudentNumber) });
    }
}

public class UserListQuery
{
    public PortalRole? Role { get; set; }

    [StringLength(100)]
    public string? Search { get; set; }

    [Range(1, 10000)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 25;
}

public class SetActiveRequest
{
    public bool IsActive { get; set; }
}

public class ResetPasswordRequest
{
    [Required, StringLength(128)]
    public string NewPassword { get; set; } = string.Empty;
}

public record UserSummaryDto(
    Guid Id,
    string Email,
    string FullName,
    string Role,
    string? StudentNumber,
    bool IsActive,
    DateTime CreatedAtUtc);
