namespace HospitalUsers.DTOs.SecurityAuditLogs
{
    // Intentionally excludes any account secrets, password hashes, and JWTs.
    public sealed class AuditLogItemDto
    {
        public long Id { get; set; }
        public string ActorStaffId { get; set; } = string.Empty;
        public string? TargetStaffId { get; set; }
        public string? TargetDepartmentId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Result { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
    }
}
