using HospitalUsers.DTOs.SecurityAuditLogs;

namespace HospitalUsers.Services.SecurityAuditLogs
{
    public sealed class AuditLogQueryResult
    {
        public AuditLogPageDto? Page { get; }
        public string? ErrorMessage { get; }

        private AuditLogQueryResult(AuditLogPageDto? page, string? errorMessage)
        {
            Page = page;
            ErrorMessage = errorMessage;
        }

        public static AuditLogQueryResult Success(AuditLogPageDto page) =>
            new(page, null);

        public static AuditLogQueryResult Invalid(string errorMessage) =>
            new(null, errorMessage);
    }
}
