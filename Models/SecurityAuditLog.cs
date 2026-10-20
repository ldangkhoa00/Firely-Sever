
using System;
using System.ComponentModel.DataAnnotations;

namespace HospitalUsers.Models
{
    public class SecurityAuditLog
    {
        [Key]
        public long Id { get; set; }

        // Nhân viên hoặc Admin thực hiện thao tác
        [Required]
        [MaxLength(20)]
        public string ActorStaffId { get; set; } = string.Empty;

        // Tài khoản chịu tác động (nếu là thao tác quản lý nhân viên)
        [MaxLength(20)]
        public string? TargetStaffId { get; set; }

        // Khoa/phòng chịu tác động (nếu là thao tác quản lý khoa/phòng)
        [MaxLength(50)]
        public string? TargetDepartmentId { get; set; }

        // Ví dụ: APPROVE_USER, CREATE_DEPARTMENT,
        // DEACTIVATE_DEPARTMENT, ACTIVATE_DEPARTMENT
        [Required]
        [MaxLength(50)]
        public string Action { get; set; } = string.Empty;

        // Kết quả: SUCCESS hoặc FAILED
        [Required]
        [MaxLength(20)]
        public string Result { get; set; } = string.Empty;

        // Thời gian thực hiện theo UTC
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
