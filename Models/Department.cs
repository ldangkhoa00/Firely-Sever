using System.ComponentModel.DataAnnotations;

namespace HospitalUsers.Models
{
    /// <summary>
    /// Khoa/phong trong benh vien.
    /// Giu nguyen mapping va rang buoc EF Core da su dung trong du an.
    /// </summary>
    public class Department
    {
        /// <summary>Ma khoa/phong, vi du: IT, ICU, ER.</summary>
        [Key]
        [Required]
        [MaxLength(50)]
        public string DepartmentId { get; set; } = string.Empty;

        /// <summary>Ten day du cua khoa/phong.</summary>
        [Required]
        [MaxLength(100)]
        public string DepartmentName { get; set; } = string.Empty;

        /// <summary>True neu khoa/phong dang hoat dong.</summary>
        public bool IsActive { get; set; } = true;
    }
}
