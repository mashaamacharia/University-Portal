using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UniversityPortal.API.Common.Constants;
using UniversityPortal.API.Common.Exceptions;
using UniversityPortal.API.Common.Responses;
using UniversityPortal.API.Common.Security;
using UniversityPortal.API.Infrastructure.Persistence;
using UniversityPortal.API.Integrations.BusinessCentral.Clients;
using UniversityPortal.API.Modules.Administration.DTOs;
using UniversityPortal.API.Modules.Administration.Models;

namespace UniversityPortal.API.Modules.Administration.Services;

public interface IUserAdminService
{
    Task<UserSummaryDto> CreateUserAsync(CreateUserRequest request, CancellationToken ct);
    Task<PagedResult<UserSummaryDto>> ListUsersAsync(UserListQuery query, CancellationToken ct);
    Task<UserSummaryDto> GetUserAsync(Guid id, CancellationToken ct);
    Task<UserSummaryDto> SetActiveAsync(Guid id, bool isActive, CancellationToken ct);
    Task ResetPasswordAsync(Guid id, string newPassword, CancellationToken ct);
}

public class UserAdminService : IUserAdminService
{
    private readonly UserManager<PortalUser> _users;
    private readonly PortalDbContext _db;
    private readonly IBusinessCentralClient _bc;
    private readonly ICurrentUser _current;
    private readonly TimeProvider _time;
    private readonly ILogger<UserAdminService> _logger;

    public UserAdminService(
        UserManager<PortalUser> users,
        PortalDbContext db,
        IBusinessCentralClient bc,
        ICurrentUser current,
        TimeProvider time,
        ILogger<UserAdminService> logger)
    {
        _users = users;
        _db = db;
        _bc = bc;
        _current = current;
        _time = time;
        _logger = logger;
    }

    private void RequireAdmin()
    {
        if (!_current.IsAuthenticated)
            throw new NotAuthenticatedException();

        if (!_current.IsInRole(Roles.Administrator))
            throw new ForbiddenException("Caller is not in the Administrator role");
    }

    public async Task<UserSummaryDto> CreateUserAsync(CreateUserRequest request, CancellationToken ct)
    {
        RequireAdmin();

        var email = request.Email.Trim();
        var role = request.Role
            ?? throw new PortalException(400, "Invalid role", "A role is required.");
        if (!Enum.IsDefined(role))
            throw new PortalException(400, "Invalid role", "The selected role is invalid.");

        var roleName = role.ToString();
        string? studentNumber = null;
        if (role == PortalRole.Student)
        {
            if (string.IsNullOrWhiteSpace(request.StudentNumber))
                throw new PortalException(400, "Invalid student number", "A student number is required.");

            var customer = await _bc.GetCustomerByNumberAsync(request.StudentNumber.Trim(), ct)
                ?? throw new PortalException(
                    422,
                    "Unknown student",
                    "No Business Central customer has that student number.");

            studentNumber = customer.Number;
            if (await _db.Users.AnyAsync(user => user.StudentNumber == studentNumber, ct))
                throw new PortalException(
                    409,
                    "Student already linked",
                    "A portal account already exists for that student number.");
        }

        if (await _users.FindByEmailAsync(email) is not null)
            throw new PortalException(409, "Email already in use", "An account with that email already exists.");

        var user = new PortalUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = request.FullName.Trim(),
            StudentNumber = studentNumber,
            CreatedAtUtc = _time.GetUtcNow().UtcDateTime
        };

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var created = await _users.CreateAsync(user, request.Password);
            if (!created.Succeeded)
                throw new PortalException(
                    400,
                    "Could not create the account",
                    string.Join(" ", created.Errors.Select(error => error.Description)));

            var inRole = await _users.AddToRoleAsync(user, roleName);
            if (!inRole.Succeeded)
            {
                throw new InvalidOperationException(
                    "Could not assign role: " +
                    string.Join("; ", inRole.Errors.Select(error => error.Description)));
            }

            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateException)
        {
            throw new PortalException(
                409,
                "Account already exists",
                "An account with that email or student number already exists.");
        }

        _logger.LogInformation(
            "Admin {AdminId} created account {UserId} with role {Role}",
            _current.UserId,
            user.Id,
            roleName);
        return ToSummary(user, roleName);
    }

    public async Task<PagedResult<UserSummaryDto>> ListUsersAsync(UserListQuery query, CancellationToken ct)
    {
        RequireAdmin();

        if (query.Role is { } role && !Enum.IsDefined(role))
            throw new PortalException(400, "Invalid role", "The selected role is invalid.");

        var rows = UsersWithRole();
        if (query.Role is { } selectedRole)
        {
            var roleName = selectedRole.ToString();
            rows = rows.Where(row => row.Role == roleName);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLower();
            rows = rows.Where(row =>
                row.User.Email!.ToLower().Contains(search) ||
                row.User.FullName.ToLower().Contains(search) ||
                (row.User.StudentNumber != null && row.User.StudentNumber.ToLower().Contains(search)));
        }

        var total = await rows.CountAsync(ct);
        var page = await rows
            .OrderBy(row => row.User.FullName)
            .ThenBy(row => row.User.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        return new PagedResult<UserSummaryDto>(
            page.Select(row => ToSummary(row.User, row.Role)).ToList(),
            query.Page,
            query.PageSize,
            total);
    }

    public async Task<UserSummaryDto> GetUserAsync(Guid id, CancellationToken ct)
    {
        RequireAdmin();

        var row = await UsersWithRole().FirstOrDefaultAsync(row => row.User.Id == id, ct)
            ?? throw new NotFoundException($"User {id} not found");
        return ToSummary(row.User, row.Role);
    }

    public async Task<UserSummaryDto> SetActiveAsync(Guid id, bool isActive, CancellationToken ct)
    {
        RequireAdmin();

        var user = await _users.FindByIdAsync(id.ToString())
            ?? throw new NotFoundException($"User {id} not found");

        if (!isActive)
        {
            if (_current.UserId == user.Id)
                throw new PortalException(400, "Not allowed", "You cannot disable your own account.");

            if (await _users.IsInRoleAsync(user, Roles.Administrator))
            {
                var admins = await _users.GetUsersInRoleAsync(Roles.Administrator);
                if (!admins.Any(admin => admin.IsActive && admin.Id != user.Id))
                {
                    throw new PortalException(
                        400,
                        "Not allowed",
                        "This is the last active administrator and cannot be disabled.");
                }
            }
        }

        user.IsActive = isActive;
        var result = await _users.UpdateAsync(user);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                "Could not update user: " +
                string.Join("; ", result.Errors.Select(error => error.Description)));
        }

        _logger.LogInformation(
            "Admin {AdminId} set account {UserId} active={IsActive}",
            _current.UserId,
            user.Id,
            isActive);

        var roles = await _users.GetRolesAsync(user);
        return ToSummary(user, roles.FirstOrDefault() ?? string.Empty);
    }

    public async Task ResetPasswordAsync(Guid id, string newPassword, CancellationToken ct)
    {
        RequireAdmin();

        var user = await _users.FindByIdAsync(id.ToString())
            ?? throw new NotFoundException($"User {id} not found");
        var token = await _users.GeneratePasswordResetTokenAsync(user);
        var result = await _users.ResetPasswordAsync(user, token, newPassword);
        if (!result.Succeeded)
        {
            throw new PortalException(
                400,
                "Could not reset the password",
                string.Join(" ", result.Errors.Select(error => error.Description)));
        }

        EnsureSucceeded(await _users.SetLockoutEndDateAsync(user, null), "Could not clear the account lockout.");
        EnsureSucceeded(await _users.ResetAccessFailedCountAsync(user), "Could not reset the failed sign-in count.");

        _logger.LogInformation(
            "Admin {AdminId} reset the password of account {UserId}",
            _current.UserId,
            user.Id);
    }

    private IQueryable<UserRow> UsersWithRole() =>
        from user in _db.Users.AsNoTracking()
        join userRole in _db.UserRoles on user.Id equals userRole.UserId
        join role in _db.Roles on userRole.RoleId equals role.Id
        select new UserRow { User = user, Role = role.Name! };

    private static UserSummaryDto ToSummary(PortalUser user, string role) =>
        new(user.Id, user.Email ?? string.Empty, user.FullName, role, user.StudentNumber, user.IsActive, user.CreatedAtUtc);

    private static void EnsureSucceeded(IdentityResult result, string message)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                message + " " + string.Join("; ", result.Errors.Select(error => error.Description)));
        }
    }

    private class UserRow
    {
        public PortalUser User { get; set; } = null!;
        public string Role { get; set; } = string.Empty;
    }
}
