using System.Data;
using System.Text.RegularExpressions;
using HospitalUsers.Data;
using HospitalUsers.DTOs.Departments;
using HospitalUsers.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace HospitalUsers.Services.Departments;

// Chứa toàn bộ quy tắc nghiệp vụ và truy cập dữ liệu khoa/phòng.
// Không trả IActionResult và không đọc trực tiếp HttpContext.
public sealed class DepartmentService : IDepartmentService
{
    private readonly AppDbContext _db;

    public DepartmentService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ActiveDepartmentResponse>> GetActiveAsync(
        CancellationToken cancellationToken = default)
    {
        return await _db.Departments
            .AsNoTracking()
            .Where(d => d.IsActive)
            .OrderBy(d => d.DepartmentName)
            .Select(d => new ActiveDepartmentResponse(d.DepartmentId, d.DepartmentName))
            .ToListAsync(cancellationToken);
    }

    public async Task<DepartmentServiceResult> CreateAsync(
        CreateDepartmentDto request,
        string? actorStaffId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(actorStaffId))
            return DepartmentServiceResult.Denied("Không xác định được nhân viên thực hiện.");

        string id = NormalizeId(request.DepartmentId);
        string name = request.DepartmentName?.Trim() ?? string.Empty;

        // Service tự bảo vệ khi gọi ngoài MVC; [ApiController] vẫn kiểm tra DTO trước đó.
        if (!IsValidId(id))
            return DepartmentServiceResult.Bad("Mã khoa/phòng không hợp lệ.");
        if (string.IsNullOrWhiteSpace(name) || name.Length > 100)
            return DepartmentServiceResult.Bad("Tên khoa/phòng không được để trống hoặc vượt quá 100 ký tự.");

        const string action = "CREATE_DEPARTMENT";
        bool exists = await _db.Departments.AsNoTracking()
            .AnyAsync(d => d.DepartmentId == id, cancellationToken);
        if (exists)
        {
            await WriteFailedAuditAsync(actorStaffId, id, action, cancellationToken);
            return DepartmentServiceResult.Conflict("Mã khoa/phòng đã tồn tại.");
        }

        var department = new Department
        {
            DepartmentId = id,
            DepartmentName = name,
            IsActive = true
        };

        try
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
            _db.Departments.Add(department);
            AddAudit(actorStaffId, id, action, "SUCCESS");
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is SqlException sqlEx &&
            (sqlEx.Number == 2601 || sqlEx.Number == 2627))
        {
            // Cuộc đua hai yêu cầu tạo trùng: không để lại bản ghi SUCCESS trong tracker.
            _db.ChangeTracker.Clear();
            await WriteFailedAuditAsync(actorStaffId, id, action, cancellationToken);
            return DepartmentServiceResult.Conflict("Mã khoa/phòng đã tồn tại.");
        }

        return DepartmentServiceResult.Created(new DepartmentCreatedResponse(
            "Tạo khoa/phòng thành công.", id, name, true));
    }

    public async Task<DepartmentServiceResult> DeactivateAsync(
        string departmentId,
        string? actorStaffId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(actorStaffId))
            return DepartmentServiceResult.Denied("Không xác định được nhân viên thực hiện.");

        string id = NormalizeId(departmentId);
        const string action = "DEACTIVATE_DEPARTMENT";

        // SERIALIZABLE bảo vệ khoảng thời gian kiểm tra nhân viên + cập nhật trạng thái.
        await using var transaction = await _db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);

        var department = await _db.Departments
            .FirstOrDefaultAsync(d => d.DepartmentId == id, cancellationToken);
        if (department == null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return DepartmentServiceResult.Missing("Không tìm thấy khoa/phòng.");
        }

        if (!department.IsActive)
        {
            AddAudit(actorStaffId, department.DepartmentId, action, "FAILED");
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return DepartmentServiceResult.Conflict("Khoa/phòng đã ngừng hoạt động.");
        }

        bool hasAssignedUsers = await _db.Users.AsNoTracking()
            .AnyAsync(u => u.DepartmentId == id, cancellationToken);
        if (hasAssignedUsers)
        {
            AddAudit(actorStaffId, department.DepartmentId, action, "FAILED");
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return DepartmentServiceResult.Conflict("Không thể ngừng hoạt động khoa/phòng đang có nhân viên.");
        }

        department.IsActive = false;
        AddAudit(actorStaffId, id, action, "SUCCESS");
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return DepartmentServiceResult.Success(new DepartmentStatusResponse(
            "Đã ngừng hoạt động khoa/phòng.", id, false));
    }

    public async Task<DepartmentServiceResult> ActivateAsync(
        string departmentId,
        string? actorStaffId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(actorStaffId))
            return DepartmentServiceResult.Denied("Không xác định được nhân viên thực hiện.");

        string id = NormalizeId(departmentId);
        const string action = "ACTIVATE_DEPARTMENT";

        await using var transaction = await _db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);

        var department = await _db.Departments
            .FirstOrDefaultAsync(d => d.DepartmentId == id, cancellationToken);
        if (department == null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return DepartmentServiceResult.Missing("Không tìm thấy khoa/phòng.");
        }

        if (department.IsActive)
        {
            AddAudit(actorStaffId, department.DepartmentId, action, "FAILED");
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return DepartmentServiceResult.Conflict("Khoa/phòng đang hoạt động.");
        }

        department.IsActive = true;
        AddAudit(actorStaffId, id, action, "SUCCESS");
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return DepartmentServiceResult.Success(new DepartmentStatusResponse(
            "Đã kích hoạt lại khoa/phòng.", id, true));
    }

    private void AddAudit(string actorStaffId, string departmentId, string action, string result)
    {
        _db.SecurityAuditLogs.Add(new SecurityAuditLog
        {
            ActorStaffId = actorStaffId,
            TargetStaffId = null,
            TargetDepartmentId = departmentId,
            Action = action,
            Result = result,
            CreatedAtUtc = DateTime.UtcNow
        });
    }

    private async Task WriteFailedAuditAsync(
        string actorStaffId,
        string departmentId,
        string action,
        CancellationToken cancellationToken)
    {
        AddAudit(actorStaffId, departmentId, action, "FAILED");
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static string NormalizeId(string? id) =>
        id?.Trim().ToUpperInvariant() ?? string.Empty;

    private static bool IsValidId(string id) =>
        id.Length is > 0 and <= 50 &&
        Regex.IsMatch(id, @"\A[A-Z0-9]+(-[A-Z0-9]+)*\z", RegexOptions.CultureInvariant);
}
