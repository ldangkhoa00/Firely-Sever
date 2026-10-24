using HospitalUsers.DTOs.SecurityAuditLogs;
using HospitalUsers.Services.SecurityAuditLogs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalUsers.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
    public sealed class SecurityAuditLogsController : ControllerBase
    {
        private readonly ISecurityAuditLogService _service;

        public SecurityAuditLogsController(ISecurityAuditLogService service)
        {
            _service = service;
        }

        // GET /api/SecurityAuditLogs/recent
        // Query string names and JSON response shape are preserved.
        [HttpGet("recent")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> GetRecentAuditLogs(
            [FromQuery] AuditLogQueryDto query,
            CancellationToken cancellationToken)
        {
            AuditLogQueryResult result = await _service.GetRecentAsync(query, cancellationToken);

            if (result.ErrorMessage is not null)
            {
                return BadRequest(new { message = result.ErrorMessage });
            }

            return Ok(result.Page);
        }
    }
}
