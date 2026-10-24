using System.Security.Claims;
using HospitalUsers.DTOs.Departments;
using HospitalUsers.Services.Departments;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalUsers.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class DepartmentsController : ControllerBase
{
    private readonly IDepartmentService _departments;

    public DepartmentsController(IDepartmentService departments)
    {
        _departments = departments;
    }

    // GET /api/Departments/active
    [HttpGet("active")]
    [AllowAnonymous]
    public async Task<IActionResult> GetActiveDepartments(CancellationToken cancellationToken)
    {
        return Ok(await _departments.GetActiveAsync(cancellationToken));
    }

    // POST /api/Departments
    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
    public async Task<IActionResult> CreateDepartment(
        [FromBody] CreateDepartmentDto request,
        CancellationToken cancellationToken)
    {
        var result = await _departments.CreateAsync(request, GetActorStaffId(), cancellationToken);
        return ToHttpResult(result);
    }

    // PUT /api/Departments/{departmentId}/deactivate
    [HttpPut("{departmentId}/deactivate")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
    public async Task<IActionResult> DeactivateDepartment(
        [FromRoute] string departmentId,
        CancellationToken cancellationToken)
    {
        var result = await _departments.DeactivateAsync(departmentId, GetActorStaffId(), cancellationToken);
        return ToHttpResult(result);
    }

    // PUT /api/Departments/{departmentId}/activate
    [HttpPut("{departmentId}/activate")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
    public async Task<IActionResult> ActivateDepartment(
        [FromRoute] string departmentId,
        CancellationToken cancellationToken)
    {
        var result = await _departments.ActivateAsync(departmentId, GetActorStaffId(), cancellationToken);
        return ToHttpResult(result);
    }

    private string? GetActorStaffId() =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    private IActionResult ToHttpResult(DepartmentServiceResult result) => result.Kind switch
    {
        DepartmentResultKind.Ok => Ok(result.Body),
        DepartmentResultKind.Created => StatusCode(StatusCodes.Status201Created, result.Body),
        DepartmentResultKind.BadRequest => BadRequest(result.Body),
        DepartmentResultKind.Unauthorized => Unauthorized(result.Body),
        DepartmentResultKind.NotFound => NotFound(result.Body),
        DepartmentResultKind.Conflict => Conflict(result.Body),
        _ => StatusCode(StatusCodes.Status500InternalServerError)
    };
}
