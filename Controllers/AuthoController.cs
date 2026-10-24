using System.Security.Claims;
using HospitalUsers.DTOs.Auth;
using HospitalUsers.Services.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HospitalUsers.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService) => _authService = authService;

    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
        => ToHttpResult(await _authService.RegisterAsync(request, cancellationToken));

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request,
        CancellationToken cancellationToken)
        => ToHttpResult(await _authService.LoginAsync(request, cancellationToken));

    [HttpPut("approve/{staffId}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
    public async Task<IActionResult> ApproveUser(string staffId, CancellationToken cancellationToken)
        => ToHttpResult(await _authService.ApproveUserAsync(staffId, ActorStaffId(), cancellationToken));

    [HttpPut("revoke-sessions/{staffId}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
    public async Task<IActionResult> RevokeSessions(string staffId, CancellationToken cancellationToken)
        => ToHttpResult(await _authService.RevokeSessionsAsync(staffId, ActorStaffId(), cancellationToken));

    [HttpPut("suspend/{staffId}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
    public async Task<IActionResult> SuspendUser(string staffId, CancellationToken cancellationToken)
        => ToHttpResult(await _authService.SuspendUserAsync(staffId, ActorStaffId(), cancellationToken));

    [HttpPut("unsuspend/{staffId}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
    public async Task<IActionResult> UnsuspendUser(string staffId, CancellationToken cancellationToken)
        => ToHttpResult(await _authService.UnsuspendUserAsync(staffId, ActorStaffId(), cancellationToken));

    private string? ActorStaffId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    private IActionResult ToHttpResult(AuthServiceResult result)
    {
        int statusCode = result.Kind switch
        {
            AuthResultKind.Success => StatusCodes.Status200OK,
            AuthResultKind.BadRequest => StatusCodes.Status400BadRequest,
            AuthResultKind.Unauthorized => StatusCodes.Status401Unauthorized,
            AuthResultKind.Forbidden => StatusCodes.Status403Forbidden,
            AuthResultKind.NotFound => StatusCodes.Status404NotFound,
            AuthResultKind.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        object payload = result.Token is not null
            ? new AuthTokenResponse(result.Token)
            : result.NewStatus is not null
                ? new AuthStatusResponse(result.Message ?? string.Empty, result.NewStatus)
                : new AuthMessageResponse(result.Message ?? string.Empty);

        return StatusCode(statusCode, payload);
    }
}
