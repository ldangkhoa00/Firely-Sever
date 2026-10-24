using HospitalUsers.Data;
using HospitalUsers.DTOs.SecurityAuditLogs;
using HospitalUsers.Models;
using Microsoft.EntityFrameworkCore;

namespace HospitalUsers.Services.SecurityAuditLogs
{
    public sealed class SecurityAuditLogService : ISecurityAuditLogService
    {
        private const int MaxPage = 10000;
        private const int MaxPageSize = 50;
        private readonly AppDbContext _context;

        public SecurityAuditLogService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<AuditLogQueryResult> GetRecentAsync(
            AuditLogQueryDto request,
            CancellationToken cancellationToken = default)
        {
            if (request.Page < 1 || request.Page > MaxPage)
            {
                return AuditLogQueryResult.Invalid(
                    "Số trang phải nằm trong khoảng từ 1 đến 10000.");
            }

            if (request.PageSize < 1 || request.PageSize > MaxPageSize)
            {
                return AuditLogQueryResult.Invalid(
                    "Số nhật ký mỗi trang phải từ 1 đến 50.");
            }

            // Validate all inputs before issuing any database query.
            string? action = Normalize(request.Action);
            string? targetDepartmentId = Normalize(request.TargetDepartmentId);
            string? actorStaffId = Normalize(request.ActorStaffId);
            string? targetStaffId = Normalize(request.TargetStaffId);
            string? result = Normalize(request.Result);

            if (action is not null && action.Length > 50)
                return AuditLogQueryResult.Invalid("Tên hành động không được vượt quá 50 ký tự.");

            if (targetDepartmentId is not null && targetDepartmentId.Length > 50)
                return AuditLogQueryResult.Invalid("Mã khoa/phòng không được vượt quá 50 ký tự.");

            if (actorStaffId is not null && actorStaffId.Length > 20)
                return AuditLogQueryResult.Invalid("Mã nhân viên thực hiện không được vượt quá 20 ký tự.");

            if (targetStaffId is not null && targetStaffId.Length > 20)
                return AuditLogQueryResult.Invalid("Mã nhân viên chịu tác động không được vượt quá 20 ký tự.");

            if (result is not null && result.Length > 20)
                return AuditLogQueryResult.Invalid("Kết quả nhật ký không được vượt quá 20 ký tự.");

            if (request.FromUtc.HasValue && request.ToUtc.HasValue &&
                request.FromUtc.Value > request.ToUtc.Value)
            {
                return AuditLogQueryResult.Invalid(
                    "Thời gian bắt đầu không được lớn hơn thời gian kết thúc.");
            }

            IQueryable<SecurityAuditLog> query = _context.SecurityAuditLogs.AsNoTracking();

            if (action is not null)
                query = query.Where(log => log.Action == action);

            if (targetDepartmentId is not null)
                query = query.Where(log => log.TargetDepartmentId == targetDepartmentId);

            if (actorStaffId is not null)
                query = query.Where(log => log.ActorStaffId == actorStaffId);

            if (targetStaffId is not null)
                query = query.Where(log => log.TargetStaffId == targetStaffId);

            if (result is not null)
                query = query.Where(log => log.Result == result);

            if (request.FromUtc.HasValue)
            {
                DateTime fromTimeUtc = request.FromUtc.Value.UtcDateTime;
                query = query.Where(log => log.CreatedAtUtc >= fromTimeUtc);
            }

            if (request.ToUtc.HasValue)
            {
                DateTime toTimeUtc = request.ToUtc.Value.UtcDateTime;
                query = query.Where(log => log.CreatedAtUtc <= toTimeUtc);
            }

            // Count only after applying every filter, before paging.
            int totalCount = await query.CountAsync(cancellationToken);

            List<AuditLogItemDto> items = await query
                .OrderByDescending(log => log.Id)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(log => new AuditLogItemDto
                {
                    Id = log.Id,
                    ActorStaffId = log.ActorStaffId,
                    TargetStaffId = log.TargetStaffId,
                    TargetDepartmentId = log.TargetDepartmentId,
                    Action = log.Action,
                    Result = log.Result,
                    CreatedAtUtc = log.CreatedAtUtc
                })
                .ToListAsync(cancellationToken);

            return AuditLogQueryResult.Success(new AuditLogPageDto
            {
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize),
                Items = items
            });
        }

        private static string? Normalize(string? input)
        {
            return string.IsNullOrWhiteSpace(input)
                ? null
                : input.Trim().ToUpperInvariant();
        }
    }
}
