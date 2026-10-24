using System.ComponentModel.DataAnnotations;

namespace HospitalUsers.Models
{
    /// <summary>
    /// Tai khoan nhan vien benh vien.
    /// Giu nguyen cac thuoc tinh, kieu du lieu va Data Annotations cua model hien tai
    /// de khong tu y thay doi schema SQL Server hoac cac Migration da co.
    /// </summary>
    public class AppUser
    {
        [Key]
        public int Id { get; set; }

        // Email la tuy chon. AppDbContext cau hinh unique index khi gia tri khac NULL.
        [MaxLength(254)]
        public string? HospitalEmail { get; set; }

        // Ma nhan vien. Dinh dang BV-... duoc kiem tra tai DTO dang ky.
        [Required]
        [MaxLength(20)]
        public string StaffId { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        // Chi luu ma bam BCrypt, khong luu mat khau dang ro.
        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        // Gia tri Role duoc quan ly boi API: Admin, Doctor, Nurse, IT.
        [Required]
        public string Role { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        // Ma khoa/phong (DepartmentId), khong phai ma phong CSI.
        public string DepartmentId { get; set; } = string.Empty;

        // PENDING_APPROVAL, ACTIVE, SUSPENDED theo nghiep vu hien tai.
        public string Status { get; set; } = "PENDING_APPROVAL";

        // Phong CSI ma Nurse duoc phan cong. Rong => chua duoc phep doc CSI.
        public string ManagedRoom { get; set; } = string.Empty;

        // Tang gia tri khi thu hoi JWT. AppDbContext giu mac dinh DB = 1.
        public int TokenVersion { get; set; } = 1;
    }
}
