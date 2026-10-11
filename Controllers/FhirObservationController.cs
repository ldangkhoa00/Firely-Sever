// file này dùng để nhận các thông tin từ esp32 gửi lên và chuyển thành chuẩn FHIR (Observation)
using Microsoft.AspNetCore.Mvc;
using Hl7.Fhir.Model; // Đây là bộ từ điển FHIR chúng ta vừa cài
using Hl7.Fhir.Serialization; // Công cụ chuyển đổi dữ liệu
using Task = System.Threading.Tasks.Task;

namespace HospitalUsers.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FhirObservationController : ControllerBase
    {
        // --- API NÀY SẼ NHẬN DỮ LIỆU TỪ ESP32 VÀ CHUYỂN THÀNH CHUẨN FHIR ---
        [HttpPost("receive-csi")]
        public IActionResult ReceiveCsiData([FromBody] CsiInputData input)
        {
            // 1. TẠO MỘT TỜ GIẤY XÉT NGHIỆM MỚI THEO CHUẨN FHIR (Observation)
            var observation = new Observation();

            // 2. ĐIỀN THÔNG TIN VÀO TỜ GIẤY
            
            // a. Trạng thái của dữ liệu này (Ví dụ: "final" - đã chốt, dữ liệu chính thức)
            observation.Status = ObservationStatus.Final;

            // b. Mã phân loại (Code): Đây là gì? 
            // Ở đây ta tự quy ước mã "CSI-FALL" là dữ liệu sóng WiFi phát hiện té ngã
            observation.Code = new CodeableConcept
            {
                Coding = new List<Coding>
                {
                    new Coding 
                    { 
                        System = "http://hospital.vn/csi-system", // Hệ thống quy ước do mình tự đặt
                        Code = "CSI-FALL", 
                        Display = "Phát hiện té ngã qua sóng WiFi CSI" 
                    }
                }
            };

            // c. Bệnh nhân nào bị ngã? (Gắn với ID của bệnh nhân trong hệ thống)
            observation.Subject = new ResourceReference($"Patient/{input.PatientId}");

            // d. Dữ liệu sóng đo được (Value)
            // Vì sóng CSI là một chuỗi số liệu phức tạp (như "0.5, 0.6, 0.8..."), ta lưu nó dưới dạng Text (String)
            observation.Value = new FhirString(input.CsiAmplitudeString);

            // e. Thời gian ghi nhận
            observation.Effective = new FhirDateTime(DateTimeOffset.Now);

            // 3. ĐÓNG GÓI VÀ HIỂN THỊ DỮ LIỆU (Xuất ra JSON chuẩn FHIR)
            // Máy tính sẽ tự động dịch các thiết lập trên thành định dạng chuẩn quốc tế
            var serializer = new FhirJsonSerializer();
            string fhirJson = serializer.SerializeToString(observation);

            // Tạm thời chúng ta chỉ in ra màn hình để xem nó trông như thế nào
            return Ok(new 
            { 
                Message = "Đã dịch thành công sang chuẩn y tế FHIR",
                Data = fhirJson 
            });
        }
    }

    // Cái này giống như "cái rổ" để hứng dữ liệu gốc gửi từ cục ESP32 lên
    public class CsiInputData
    {
        public string PatientId { get; set; } // Mã bệnh nhân đang nằm trên giường
        public string CsiAmplitudeString { get; set; } // Chuỗi dữ liệu sóng WiFi
    }
}