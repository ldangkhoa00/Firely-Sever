using Microsoft.AspNetCore.Mvc;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using HospitalUsers.Data;
using HospitalUsers.Models;

namespace HospitalUsers.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IotController : ControllerBase
    {
        private readonly AppDbContext _context;
        public IotController(AppDbContext context) { _context = context; }

        public class EspPayload { public string Room { get; set; } = ""; public string CsiData { get; set; } = ""; }

        [HttpPost("csi")]
        public IActionResult ReceiveCsi([FromBody] EspPayload payload)
        {
            // Dùng Firely SDK bọc mảng sóng thô vào chuẩn FHIR
            var obs = new Observation
            {
                Id = Guid.NewGuid().ToString(),
                Status = ObservationStatus.Final,
                Code = new CodeableConcept("http://loinc.org", "CSI-WIFI", "WiFi Channel State"),
                Value = new FhirString(payload.CsiData)
            };

            var json = new FhirJsonSerializer().SerializeToString(obs);

            _context.CsiRecords.Add(new CsiRecord { RoomNumber = payload.Room, FhirObservationJson = json });
            _context.SaveChanges();

            return Ok("Đã nhận dữ liệu CSI");
        }
    }
}