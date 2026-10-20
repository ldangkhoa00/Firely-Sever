
using System.ComponentModel.DataAnnotations;

namespace HospitalUsers.Models
{
    public class Department
    {
        // Mã khoa/phòng, ví dụ: IT, ICU, ER
        [Key]
        [Required]
        [MaxLength(50)]
        public string DepartmentId { get; set; } = string.Empty;

        // Tên đầy đủ của khoa/phòng
        [Required]
        [MaxLength(100)]
        public string DepartmentName { get; set; } = string.Empty;

        // Khoa/phòng có đang hoạt động không?
        public bool IsActive { get; set; } = true;
    }
}
