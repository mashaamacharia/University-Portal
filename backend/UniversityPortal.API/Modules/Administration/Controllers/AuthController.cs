using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using UniversityPortal.API.Common.Constants;
using UniversityPortal.API.Modules.Administration.DTOs;
using UniversityPortal.API.Modules.Administration.Services;

namespace UniversityPortal.API.Modules.Administration.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService auth) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request) =>
        Ok(await auth.LoginAsync(request));

    [HttpGet("me")]
    public ActionResult<CurrentUserDto> Me()
    {
        if (!Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId))
            return Unauthorized();

        return Ok(new CurrentUserDto(
            userId,
            User.FindFirstValue(ClaimNames.Name) ?? string.Empty,
            User.FindFirstValue(JwtRegisteredClaimNames.Email) ?? string.Empty,
            User.FindAll(ClaimNames.Role).Select(claim => claim.Value).ToList(),
            User.FindFirstValue(ClaimNames.StudentNumber)));
    }
}
