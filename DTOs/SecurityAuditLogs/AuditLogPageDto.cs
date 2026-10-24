namespace HospitalUsers.DTOs.SecurityAuditLogs
{
    // ASP.NET Core web JSON serialization produces:
    // page, pageSize, totalCount, totalPages, items.
    public sealed class AuditLogPageDto
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public List<AuditLogItemDto> Items { get; set; } = new();
    }
}
