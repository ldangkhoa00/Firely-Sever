using HospitalUsers.DTOs.Iot;
using HospitalUsers.Security;
using HospitalUsers.Services.Iot;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HospitalUsers.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public sealed class IotController : ControllerBase
    {
        private readonly IIotCsiService _iotCsiService;

        public IotController(IIotCsiService iotCsiService)
        {
            _iotCsiService = iotCsiService;
        }

        // POST /api/Iot/csi: keep the existing route, auth scheme and response.
        [HttpPost("csi")]
        [Authorize(AuthenticationSchemes = DeviceApiKeyAuthenticationHandler.SchemeName)]
        [EnableRateLimiting("device-csi")]
        [RequestSizeLimit(256 * 1024)]
        public async Task<IActionResult> ReceiveCsi(
            [FromBody] EspCsiPayloadDto? payload,
            CancellationToken cancellationToken)
        {
            if (payload is null)
            {
                return BadRequest(new { message = "Thiếu dữ liệu CSI." });
            }

            IotCsiResult result = await _iotCsiService.ReceiveAsync(
                payload, cancellationToken);

            return result switch
            {
                IotCsiResult.Received => Ok("Đã nhận dữ liệu CSI"),
                IotCsiResult.MissingRoom => BadRequest(new
                {
                    message = "Mã phòng không được để trống."
                }),
                IotCsiResult.MissingCsiData => BadRequest(new
                {
                    message = "Dữ liệu CSI không được để trống."
                }),
                IotCsiResult.InvalidRoomLength => BadRequest(new
                {
                    message = "Mã phòng không được vượt quá 50 ký tự."
                }),
                IotCsiResult.InvalidCsiLength => BadRequest(new
                {
                    message = "Dữ liệu CSI vượt quá kích thước cho phép."
                }),
                IotCsiResult.StorageFailed => StatusCode(500, new
                {
                    message = "Không thể lưu dữ liệu CSI."
                }),
                _ => StatusCode(500, new
                {
                    message = "Không thể xử lý dữ liệu CSI."
                })
            };
        }
    }
}
