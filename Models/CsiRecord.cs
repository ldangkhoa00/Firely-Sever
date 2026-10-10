using System.ComponentModel.DataAnnotations;

namespace HospitalUsers.Models
{
    public class CsiRecord
    {
        [Key]
        public int Id { get; set; }
        
        public string RoomNumber { get; set; } = string.Empty; // Biết ESP32 gửi từ phòng nào
        
        public string FhirObservationJson { get; set; } = string.Empty; // Chuỗi JSON chuẩn y tế
        
        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    }
}