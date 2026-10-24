namespace HospitalUsers.DTOs.SecurityAuditLogs
{
    // Bound from the existing GET /api/SecurityAuditLogs/recent query string.
    // Validation lives in the Service to preserve the existing error messages.
    public sealed class AuditLogQueryDto
    {
        public string? Action { get; set; }
        public string? TargetDepartmentId { get; set; }
        public string? ActorStaffId { get; set; }
        public string? TargetStaffId { get; set; }
        public string? Result { get; set; }
        public DateTimeOffset? FromUtc { get; set; }
        public DateTimeOffset? ToUtc { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
