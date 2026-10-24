using System.ComponentModel.DataAnnotations;

namespace HospitalUsers.DTOs.Auth;

/// <summary>Du lieu dang ky. Ten JSON va Validation giu tuong thich API cu.</summary>
public sealed class RegisterRequest
{
    [Required(ErrorMessage = "Mã nhân viên không được để trống.")]
    [MaxLength(20, ErrorMessage = "Mã nhân viên không được vượt quá 20 ký tự.")]
    [RegularExpression(@"\ABV-[A-Z0-9]+(-[A-Z0-9]+)*\z",
        ErrorMessage = "Mã nhân viên phải bắt đầu bằng BV- và chỉ chứa chữ IN HOA, số hoặc dấu gạch ngang giữa các nhóm.")]
    public string StaffId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu không được để trống.")]
    [MinLength(12, ErrorMessage = "Mật khẩu phải có ít nhất 12 ký tự.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Họ tên nhân viên không được để trống.")]
    [MaxLength(100, ErrorMessage = "Họ tên nhân viên không được vượt quá 100 ký tự.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email bệnh viện không được để trống.")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    [MaxLength(254, ErrorMessage = "Email không được vượt quá 254 ký tự.")]
    [RegularExpression(@"^[^@\s]+@hospital\.example$",
        ErrorMessage = "Email phải thuộc tên miền @hospital.example.")]
    public string HospitalEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "Số điện thoại không được để trống.")]
    [MaxLength(10, ErrorMessage = "Số điện thoại không được vượt quá 10 ký tự.")]
    [RegularExpression(@"^0[35789][0-9]{8}$",
        ErrorMessage = "Số di động phải có đúng 10 chữ số và bắt đầu bằng 03, 05, 07, 08 hoặc 09.")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mã khoa/phòng không được để trống.")]
    [MaxLength(50, ErrorMessage = "Mã khoa/phòng không được vượt quá 50 ký tự.")]
    public string DepartmentId { get; set; } = string.Empty;

    // Khong cho phep nguoi dang ky tu cap quyen Admin/IT.
    public string Role { get; set; } = "Nurse";
}
