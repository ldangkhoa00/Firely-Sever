using System.ComponentModel.DataAnnotations;

namespace HospitalUsers.Models
{
    /// <summary>
    /// Thong tin co ban cua benh nhan dang duoc quan ly trong benh vien.
    ///
    /// Luu y ve tuong thich:
    /// - Giu nguyen 3 thuoc tinh, kieu du lieu va Data Annotations hien tai.
    /// - Khong them cot, gioi han do dai hoac rang buoc SQL moi.
    /// - Xac thuc nghiep vu (ma phong hop le, quyen truy cap, FHIR Patient ID)
    ///   can duoc xu ly tai API/service tuong ung.
    /// </summary>
    public class Patient
    {
        /// <summary>Khoa chinh cua benh nhan.</summary>
        [Key]
        public int Id { get; set; }

        /// <summary>Ho va ten; bat buoc khi tiep nhan du lieu.</summary>
        [Required]
        public string FullName { get; set; } = string.Empty;

        /// <summary>Ma phong hien tai; bat buoc khi tiep nhan du lieu.</summary>
        [Required]
        public string RoomNumber { get; set; } = string.Empty;
    }
}
