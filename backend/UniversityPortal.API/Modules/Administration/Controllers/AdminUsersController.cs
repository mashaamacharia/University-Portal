using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniversityPortal.API.Common.Constants;
using UniversityPortal.API.Common.Responses;
using UniversityPortal.API.Modules.Administration.DTOs;
using UniversityPortal.API.Modules.Administration.Services;

namespace UniversityPortal.API.Modules.Administration.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = Roles.Administrator)]
public class AdminUsersController(IUserAdminService users) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<UserSummaryDto>> Create(CreateUserRequest request, CancellationToken ct)
    {
        var created = await users.CreateUserAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<UserSummaryDto>>> List(
        [FromQuery] UserListQuery query,
        CancellationToken ct) =>
        Ok(await users.ListUsersAsync(query, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserSummaryDto>> Get(Guid id, CancellationToken ct) =>
        Ok(await users.GetUserAsync(id, ct));

    [HttpPut("{id:guid}/active")]
    public async Task<ActionResult<UserSummaryDto>> SetActive(
        Guid id,
        SetActiveRequest request,
        CancellationToken ct) =>
        Ok(await users.SetActiveAsync(id, request.IsActive, ct));

    [HttpPost("{id:guid}/reset-password")]
    public async Task<IActionResult> ResetPassword(
        Guid id,
        ResetPasswordRequest request,
        CancellationToken ct)
    {
        await users.ResetPasswordAsync(id, request.NewPassword, ct);
        return NoContent();
    }
}
