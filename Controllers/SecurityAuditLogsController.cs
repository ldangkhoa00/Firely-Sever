
using HospitalUsers.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HospitalUsers.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class SecurityAuditLogsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SecurityAuditLogsController(AppDbContext context)
        {
            _context = context;
        }

        // Chỉ Admin được xem 50 nhật ký gần nhất
        
        
        

        [HttpGet("recent")]
        public IActionResult GetRecentAuditLogs(
            [FromQuery] string? action,
            [FromQuery] string? targetDepartmentId,
            [FromQuery] string? actorStaffId,
            [FromQuery] string? targetStaffId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)

            {
                
            // Kiểm tra số trang hợp lệ
            if (page < 1 || page > 10000)
            {
                return BadRequest(new
                {
                    message = "Số trang phải nằm trong khoảng từ 1 đến 10000."
                });
            }

            // Mỗi trang cho phép tối đa 50 nhật ký
            if (pageSize < 1 || pageSize > 50)
            {
                return BadRequest(new
                {
                    message = "Số nhật ký mỗi trang phải từ 1 đến 50."
                });
            }

            // 1. Tạo truy vấn đọc nhật ký
            var query = _context.SecurityAuditLogs
                .AsNoTracking();

            // 2. Lọc theo hành động (nếu có)
            if (!string.IsNullOrWhiteSpace(action))
            {
                string normalizedAction =
                    action.Trim().ToUpperInvariant();

                if (normalizedAction.Length > 50)
                {
                    return BadRequest(new
                    {
                        message = "Tên hành động không được vượt quá 50 ký tự."
                    });
                }

                query = query.Where(log =>
                    log.Action == normalizedAction);
            }

            // 3. Lọc theo mã khoa/phòng (nếu có)
            if (!string.IsNullOrWhiteSpace(targetDepartmentId))
            {
                string normalizedDepartmentId =
                    targetDepartmentId.Trim().ToUpperInvariant();

                if (normalizedDepartmentId.Length > 50)
                {
                    return BadRequest(new
                    {
                        message = "Mã khoa/phòng không được vượt quá 50 ký tự."
                    });
                }

                query = query.Where(log =>
                    log.TargetDepartmentId == normalizedDepartmentId);
            }

            
            // Lọc theo nhân viên thực hiện thao tác (nếu có)
            if (!string.IsNullOrWhiteSpace(actorStaffId))
            {
                string normalizedActorStaffId =
                    actorStaffId.Trim().ToUpperInvariant();

                // Mã nhân viên tối đa 20 ký tự
                if (normalizedActorStaffId.Length > 20)
                {
                    return BadRequest(new
                    {
                        message = "Mã nhân viên thực hiện không được vượt quá 20 ký tự."
                    });
                }

                query = query.Where(log =>
                    log.ActorStaffId == normalizedActorStaffId);
            }


            // Lọc theo nhân viên chịu tác động
            if (!string.IsNullOrWhiteSpace(targetStaffId))
            {
                string normalizedTargetStaffId =
                    targetStaffId.Trim().ToUpperInvariant();

                if (normalizedTargetStaffId.Length > 20)
                {
                    return BadRequest(new
                    {
                        message = "Mã nhân viên chịu tác động không được vượt quá 20 ký tự."
                    });
                }

                query = query.Where(log =>
                    log.TargetStaffId == normalizedTargetStaffId);
            }


            // 4. Lấy tối đa 50 nhật ký gần nhất
            var logs = query
                .OrderByDescending(log => log.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)

                .Select(log => new
                {
                    log.Id,
                    log.ActorStaffId,
                    log.TargetStaffId,
                    log.TargetDepartmentId,
                    log.Action,
                    log.Result,
                    log.CreatedAtUtc
                })
                .ToList();

            return Ok(logs);
        }

    }
}
