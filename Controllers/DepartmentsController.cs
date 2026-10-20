
using HospitalUsers.Data;
using Microsoft.AspNetCore.Authorization;
using HospitalUsers.Models;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HospitalUsers.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DepartmentsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DepartmentsController(AppDbContext context)
        {
            _context = context;
        }

        // Lấy danh sách khoa/phòng đang hoạt động
        [HttpGet("active")]
        [AllowAnonymous]
        public IActionResult GetActiveDepartments()
        {
            var departments = _context.Departments
                .AsNoTracking()
                .Where(d => d.IsActive)
                .OrderBy(d => d.DepartmentName)
                .Select(d => new
                {
                    d.DepartmentId,
                    d.DepartmentName
                })
                .ToList();

            return Ok(departments);
        }
        
        // API chỉ dành cho Admin tạo khoa/phòng mới
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult CreateDepartment(
            [FromBody] CreateDepartmentRequest request)
        {
            // 1. Chuẩn hóa dữ liệu đầu vào
            string normalizedDepartmentId =
                request.DepartmentId.Trim().ToUpperInvariant();

            string normalizedDepartmentName =
                request.DepartmentName.Trim();

            // 2. Kiểm tra tên khoa/phòng
            if (string.IsNullOrWhiteSpace(normalizedDepartmentName))
            {
                return BadRequest(new
                {
                    message = "Tên khoa/phòng không được để trống."
                });
            }

            // 3. Kiểm tra mã khoa/phòng đã tồn tại chưa
            if (_context.Departments.Any(d =>
                d.DepartmentId == normalizedDepartmentId))
            {
                return Conflict(new
                {
                    message = "Mã khoa/phòng đã tồn tại."
                });
            }

            // 4. Tạo khoa/phòng mới
            var department = new Department
            {
                DepartmentId = normalizedDepartmentId,
                DepartmentName = normalizedDepartmentName,
                IsActive = true
            };

           
            // 5. Xác định Admin đang thực hiện thao tác
            string? actorStaffId = User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier
            )?.Value;

            if (string.IsNullOrWhiteSpace(actorStaffId))
            {
                return Unauthorized(new
                {
                    message = "Không xác định được nhân viên thực hiện."
                });
            }

            // 6. Chuẩn bị tạo khoa/phòng mới
            _context.Departments.Add(department);

            // 7. Bắt đầu Transaction
            using var transaction = _context.Database.BeginTransaction();

            try
            {
                // 8. Ghi nhật ký bảo mật
                var auditLog = new SecurityAuditLog
                {
                    ActorStaffId = actorStaffId,
                    TargetStaffId = null,
                    TargetDepartmentId = department.DepartmentId,
                    Action = "CREATE_DEPARTMENT",
                    Result = "SUCCESS",
                    CreatedAtUtc = DateTime.UtcNow
                };

                _context.SecurityAuditLogs.Add(auditLog);

                // 9. Lưu khoa/phòng và nhật ký cùng nhau
                _context.SaveChanges();

                // 10. Xác nhận Transaction thành công
                transaction.Commit();
            }
            catch (DbUpdateException ex) when (
                ex.InnerException is SqlException sqlEx &&
                (sqlEx.Number == 2601 || sqlEx.Number == 2627))
            {
                transaction.Rollback();

                return Conflict(new
                {
                    message = "Mã khoa/phòng đã tồn tại."
                });
            }


            // 6. Tạo thành công
            return StatusCode(201, new
            {
                message = "Tạo khoa/phòng thành công.",
                departmentId = department.DepartmentId,
                departmentName = department.DepartmentName,
                isActive = department.IsActive
            });
        }
        
        // API: Admin ngừng hoạt động một khoa/phòng
        [HttpPut("{departmentId}/deactivate")]
        [Authorize(Roles = "Admin")]
        public IActionResult DeactivateDepartment(string departmentId)
        {
            // 1. Chuẩn hóa mã khoa/phòng
            string normalizedDepartmentId =
                departmentId.Trim().ToUpperInvariant();

            // 2. Tìm khoa/phòng trong database
            var department = _context.Departments
                .FirstOrDefault(d =>
                    d.DepartmentId == normalizedDepartmentId);

            if (department == null)
            {
                return NotFound(new
                {
                    message = "Không tìm thấy khoa/phòng."
                });
            }

            // 3. Kiểm tra khoa/phòng đã ngừng hoạt động chưa
            if (!department.IsActive)
            {
                return Conflict(new
                {
                    message = "Khoa/phòng đã ngừng hoạt động."
                });
            }

            // 4. Không cho ngừng hoạt động nếu còn nhân viên được gán
            bool hasAssignedUsers = _context.Users.Any(u =>
                u.DepartmentId == normalizedDepartmentId);

            if (hasAssignedUsers)
            {
                return Conflict(new
                {
                    message = "Không thể ngừng hoạt động khoa/phòng đang có nhân viên."
                });
            }

            // 5. Xác định Admin đang thực hiện thao tác
            string? actorStaffId = User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier
            )?.Value;

            if (string.IsNullOrWhiteSpace(actorStaffId))
            {
                return Unauthorized(new
                {
                    message = "Không xác định được nhân viên thực hiện."
                });
            }

            // 6. Bắt đầu Transaction
            using var transaction = _context.Database.BeginTransaction();

            // 7. Ngừng hoạt động khoa/phòng
            department.IsActive = false;

            // 8. Tạo nhật ký bảo mật
            var auditLog = new SecurityAuditLog
            {
                ActorStaffId = actorStaffId,
                TargetStaffId = null,
                TargetDepartmentId = department.DepartmentId,
                Action = "DEACTIVATE_DEPARTMENT",
                Result = "SUCCESS",
                CreatedAtUtc = DateTime.UtcNow
            };

            _context.SecurityAuditLogs.Add(auditLog);

            // 9. Lưu trạng thái và nhật ký cùng nhau
            _context.SaveChanges();

            // 10. Xác nhận Transaction
            transaction.Commit();

            return Ok(new
            {
                message = "Đã ngừng hoạt động khoa/phòng.",
                departmentId = department.DepartmentId,
                isActive = department.IsActive
            });

        }
        
        // API: Chỉ Admin được kích hoạt lại khoa/phòng
        [HttpPut("{departmentId}/activate")]
        [Authorize(Roles = "Admin")]
        public IActionResult ActivateDepartment(string departmentId)
        {
            // 1. Chuẩn hóa mã khoa/phòng
            string normalizedDepartmentId =
                departmentId.Trim().ToUpperInvariant();

            // 2. Tìm khoa/phòng trong SQL Server
            var department = _context.Departments
                .FirstOrDefault(d =>
                    d.DepartmentId == normalizedDepartmentId);

            // 3. Nếu không tìm thấy
            if (department == null)
            {
                return NotFound(new
                {
                    message = "Không tìm thấy khoa/phòng."
                });
            }

            // 4. Nếu khoa/phòng đang hoạt động rồi
            if (department.IsActive)
            {
                return Conflict(new
                {
                    message = "Khoa/phòng đang hoạt động."
                });
            }


            // 5. Xác định Admin thực hiện thao tác
            string? actorStaffId = User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier
            )?.Value;

            if (string.IsNullOrWhiteSpace(actorStaffId))
            {
                return Unauthorized(new
                {
                    message = "Không xác định được nhân viên thực hiện."
                });
            }

            // 6. Bắt đầu Transaction
            using var transaction = _context.Database.BeginTransaction();

            // 7. Kích hoạt lại khoa/phòng
            department.IsActive = true;

            // 8. Tạo nhật ký bảo mật
            var auditLog = new SecurityAuditLog
            {
                ActorStaffId = actorStaffId,
                TargetStaffId = null,
                TargetDepartmentId = department.DepartmentId,
                Action = "ACTIVATE_DEPARTMENT",
                Result = "SUCCESS",
                CreatedAtUtc = DateTime.UtcNow
            };

            _context.SecurityAuditLogs.Add(auditLog);

            // 9. Lưu trạng thái và nhật ký cùng nhau
            _context.SaveChanges();

            // 10. Xác nhận Transaction
            transaction.Commit();

            // 11. Trả kết quả
            return Ok(new
            {
                message = "Đã kích hoạt lại khoa/phòng.",
                departmentId = department.DepartmentId,
                isActive = department.IsActive
            });

        }

    }
}
