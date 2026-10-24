using System.Security.Claims;
using HospitalUsers.DTOs.Csi;
using HospitalUsers.Services.Csi;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalUsers.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(
        AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
        Roles = "Doctor,Nurse")]
    public class CsiController : ControllerBase
    {
        private readonly ICsiService _csiService;

        public CsiController(ICsiService csiService)
        {
            _csiService = csiService;
        }

        // Keep route, query parameter, response shape and auth policy unchanged.
        [HttpGet("room/{roomNumber}")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> GetCsi(
            [FromRoute] string roomNumber,
            CancellationToken cancellationToken,
            [FromQuery] int limit = CsiRoomQueryDto.DefaultLimit)
        {
            bool isNurse = User.IsInRole("Nurse");
            string? staffId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            CsiRoomResult result = await _csiService.GetRoomAsync(
                new CsiRoomQueryDto(roomNumber, limit),
                staffId,
                isNurse,
                cancellationToken);

            return result.Status switch
            {
                CsiRoomResultStatus.InvalidRoom => BadRequest(new
                {
                    message = "Mã phòng phải có từ 1 đến 50 ký tự."
                }),
                CsiRoomResultStatus.InvalidLimit => BadRequest(new
                {
                    message = "limit phải nằm trong khoảng từ 1 đến 100."
                }),
                CsiRoomResultStatus.Forbidden => Forbid(),
                CsiRoomResultStatus.Success => Ok(result.Records),
                _ => throw new InvalidOperationException("Unexpected CSI query result.")
            };
        }
    }
}
