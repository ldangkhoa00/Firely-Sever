using System.ComponentModel.DataAnnotations;

namespace HospitalUsers.DTOs.Departments;

// Request DTO. Giữ nguyên tên các trường JSON và quy tắc kiểm tra như API cũ.
public sealed class CreateDepartmentDto
{
    [Required(ErrorMessage = "Mã khoa/phòng không được để trống.")]
    [MaxLength(50, ErrorMessage = "Mã khoa/phòng không được vượt quá 50 ký tự.")]
    [RegularExpression(@"^[A-Za-z0-9]+(-[A-Za-z0-9]+)*$",
        ErrorMessage = "Mã khoa/phòng chỉ được chứa chữ, số và dấu gạch ngang giữa các nhóm.")]
    public string DepartmentId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tên khoa/phòng không được để trống.")]
    [MaxLength(100, ErrorMessage = "Tên khoa/phòng không được vượt quá 100 ký tự.")]
    public string DepartmentName { get; set; } = string.Empty;
}
