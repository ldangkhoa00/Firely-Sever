using System;
using System.ComponentModel.DataAnnotations;

namespace HospitalUsers.Models
{
    /// <summary>
    /// Ban ghi nhat ky bao mat cho cac thao tac quan tri.
    /// Luu tru trong bang SecurityAuditLogs thong qua EF Core.
    /// </summary>
    public class SecurityAuditLog
    {
        // Khoa chinh. Giu nguyen kieu long de tuong thich database.
        [Key]
        public long Id { get; set; }

        // Ma nhan vien thuc hien thao tac (lay tu danh tinh da xac thuc).
        [Required]
        [MaxLength(20)]
        public string ActorStaffId { get; set; } = string.Empty;

        // Ma nhan vien chiu tac dong; null voi thao tac khong lien quan tai khoan.
        [MaxLength(20)]
        public string? TargetStaffId { get; set; }

        // Ma khoa/phong chiu tac dong; null voi thao tac khong lien quan khoa/phong.
        [MaxLength(50)]
        public string? TargetDepartmentId { get; set; }

        // Vi du: APPROVE_USER, SUSPEND_USER, CREATE_DEPARTMENT.
        [Required]
        [MaxLength(50)]
        public string Action { get; set; } = string.Empty;

        // Gia tri nghiep vu do controller thiet lap: SUCCESS hoac FAILED.
        [Required]
        [MaxLength(20)]
        public string Result { get; set; } = string.Empty;

        // Thoi diem ghi nhan UTC. Giu nguyen kieu DateTime va gia tri mac dinh.
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
