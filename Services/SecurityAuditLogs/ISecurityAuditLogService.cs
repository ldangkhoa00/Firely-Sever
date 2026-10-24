using HospitalUsers.DTOs.SecurityAuditLogs;

namespace HospitalUsers.Services.SecurityAuditLogs
{
    public interface ISecurityAuditLogService
    {
        Task<AuditLogQueryResult> GetRecentAsync(
            AuditLogQueryDto query,
            CancellationToken cancellationToken = default);
    }
}
