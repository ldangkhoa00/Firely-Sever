using HospitalUsers.DTOs.FhirObservation;
using HospitalUsers.Security;
using HospitalUsers.Services.FhirObservation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HospitalUsers.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public sealed class FhirObservationController : ControllerBase
    {
        private readonly IFhirObservationService _service;

        public FhirObservationController(IFhirObservationService service)
        {
            _service = service;
        }

        // POST /api/FhirObservation/receive-csi
        // The endpoint converts CSI into a FHIR R4 Observation; it does NOT persist to SQL Server.
        [HttpPost("receive-csi")]
        [Authorize(AuthenticationSchemes = DeviceApiKeyAuthenticationHandler.SchemeName)]
        [EnableRateLimiting("device-csi")]
        [RequestSizeLimit(256 * 1024)]
        public IActionResult ReceiveCsiData([FromBody] CsiObservationInputDto? input)
        {
            if (input is null)
            {
                return BadRequest(new { message = "Thiếu dữ liệu CSI." });
            }

            FhirObservationResult result = _service.ConvertCsi(input);

            return result.Outcome switch
            {
                FhirObservationOutcome.Success => Ok(new FhirObservationResponseDto
                {
                    Message = "Đã dịch thành công sang chuẩn y tế FHIR",
                    Data = result.FhirJson!
                }),
                FhirObservationOutcome.MissingPatientId => BadRequest(new
                {
                    message = "Mã bệnh nhân không được để trống."
                }),
                FhirObservationOutcome.MissingCsiData => BadRequest(new
                {
                    message = "Dữ liệu CSI không được để trống."
                }),
                FhirObservationOutcome.InvalidPatientId => BadRequest(new
                {
                    message = "Mã bệnh nhân không hợp lệ (tối đa 64 ký tự: chữ, số, dấu chấm và dấu gạch ngang)."
                }),
                FhirObservationOutcome.InvalidCsiLength => BadRequest(new
                {
                    message = "Dữ liệu CSI vượt quá kích thước cho phép."
                }),
                _ => StatusCode(500, new
                {
                    message = "Không thể chuyển đổi dữ liệu CSI."
                })
            };
        }
    }
}
