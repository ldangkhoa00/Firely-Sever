using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace HospitalUsers.Models
{
    public class AppUser
    {
        [Key]
        public int Id { get; set; }
        
        
        [MaxLength(254)]
        public string? HospitalEmail { get; set; }


        // Dựa trên tài liệu bạn đang làm: Staff ID phải bắt đầu bằng BV-
        [Required]
        [MaxLength(20)]
        public string StaffId { get; set; } = string.Empty; 
        
        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;
        
        [Required]
        public string PasswordHash { get; set; } = string.Empty;
        
        [Required]
        public string Role { get; set; } = string.Empty; // "Doctor", "Nurse", "IT"
        
        // Dành riêng cho Y tá để phân quyền truy cập CSI theo phòng
        // THÊM MỚI
        public string PhoneNumber { get; set; } = string.Empty;
        public string DepartmentId { get; set; } = string.Empty; // Lưu mã phòng (VD: Khoa Cấp cứu)
        
        // Trạng thái tài khoản (Active, Pending, Suspended)
        public string Status { get; set; } = "PENDING_APPROVAL";
        public string ManagedRoom { get; set; } = string.Empty; 

        
        /// <summary>
        /// Phiên bản xác thực của tài khoản.
        /// Tăng giá trị này khi cần thu hồi các JWT đã cấp.
        /// </summary>
        public int TokenVersion { get; set; } = 1;

    }
}