using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using HospitalUsers.Data;
using HospitalUsers.DTOs.Auth;
using HospitalUsers.Models;
using HospitalUsers.Security;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace HospitalUsers.Services.Auth;

/// <summary>
/// Nghiep vu tai khoan nhan vien. Controller chi con dinh tuyen, phan quyen HTTP
/// va chuyen ket qua. Khong doi schema SQL, route hay ten JSON hien tai.
/// </summary>
public sealed class AuthService : IAuthService
{
    private const string InvalidLoginMessage = "Mã nhân viên hoặc mật khẩu không chính xác.";
    private const string InvalidStaffIdMessage = "Mã nhân viên không hợp lệ.";
    private static readonly Regex StaffIdRegex = new(
        @"\ABV-[A-Z0-9]+(-[A-Z0-9]+)*\z",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private readonly AppDbContext _db;
    private readonly IConfiguration _configuration;

    public AuthService(AppDbContext db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    public async Task<AuthServiceResult> RegisterAsync(
        RegisterRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string? role = request.Role?.Trim().ToLowerInvariant() switch
        {
            "nurse" => "Nurse",
            "doctor" => "Doctor",
            _ => null
        };

        if (role is null)
            return AuthServiceResult.Bad("Vai trò đăng ký không hợp lệ. Chỉ được đăng ký Nurse hoặc Doctor.");

        // [ApiController] ap dung DataAnnotations cua RegisterRequest truoc khi goi service.
        string staffId = request.StaffId.Trim();
        string departmentId = request.DepartmentId.Trim().ToUpperInvariant();
        string hospitalEmail = request.HospitalEmail.Trim().ToLowerInvariant();

        if (Encoding.UTF8.GetByteCount(request.Password) > 72)
            return AuthServiceResult.Bad("Mật khẩu không được vượt quá 72 byte UTF-8.");

        bool departmentIsActive = await _db.Departments.AsNoTracking()
            .AnyAsync(d => d.DepartmentId == departmentId && d.IsActive, cancellationToken);
        if (!departmentIsActive)
            return AuthServiceResult.Bad("Mã khoa/phòng không tồn tại hoặc đã ngừng hoạt động.");

        if (await _db.Users.AsNoTracking()
            .AnyAsync(u => u.HospitalEmail == hospitalEmail, cancellationToken))
            return AuthServiceResult.ConflictResult("Email bệnh viện đã được đăng ký.");

        if (await _db.Users.AsNoTracking()
            .AnyAsync(u => u.StaffId == staffId, cancellationToken))
            return AuthServiceResult.ConflictResult("Mã nhân viên đã được đăng ký.");

        _db.Users.Add(new AppUser
        {
            StaffId = staffId,
            PasswordHash = PasswordHasher.HashPassword(request.Password),
            FullName = request.FullName.Trim(),
            HospitalEmail = hospitalEmail,
            PhoneNumber = request.PhoneNumber,
            DepartmentId = departmentId,
            Role = role,
            Status = "PENDING_APPROVAL"
        });

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is SqlException sql &&
            (sql.Number == 2601 || sql.Number == 2627))
        {
            // Xu ly race condition dang ky trung StaffId/email theo unique index.
            _db.ChangeTracker.Clear();
            if (await _db.Users.AsNoTracking()
                .AnyAsync(u => u.StaffId == staffId, cancellationToken))
                return AuthServiceResult.ConflictResult("Mã nhân viên đã được đăng ký.");

            if (await _db.Users.AsNoTracking()
                .AnyAsync(u => u.HospitalEmail == hospitalEmail, cancellationToken))
                return AuthServiceResult.ConflictResult("Email bệnh viện đã được đăng ký.");

            throw;
        }

        return AuthServiceResult.Ok("Đăng ký thành công! Vui lòng chờ phê duyệt.");
    }

    public async Task<AuthServiceResult> LoginAsync(
        LoginRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Identifier) ||
            request.Identifier.Length > 100 ||
            string.IsNullOrEmpty(request.Password) ||
            Encoding.UTF8.GetByteCount(request.Password) > 72)
        {
            return AuthServiceResult.Denied(InvalidLoginMessage);
        }

        string staffId = request.Identifier.Trim().ToUpperInvariant();
        var user = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.StaffId == staffId, cancellationToken);

        if (user is null || !PasswordHasher.VerifyPassword(request.Password, user.PasswordHash))
            return AuthServiceResult.Denied(InvalidLoginMessage);

        if (user.Status == "PENDING_APPROVAL")
            return AuthServiceResult.ForbiddenResult("Tài khoản của bạn đang chờ Quản trị viên duyệt.");

        if (user.Status == "SUSPENDED")
            return AuthServiceResult.ForbiddenResult("Tài khoản đã bị tạm khóa.");

        if (user.Status != "ACTIVE")
            return AuthServiceResult.ForbiddenResult(
                "Trạng thái tài khoản không hợp lệ hoặc chưa được kích hoạt. Vui lòng liên hệ quản trị viên.");

        string jwtKey = _configuration["JwtSettings:Key"]
            ?? throw new InvalidOperationException("Chưa cấu hình JwtSettings:Key.");

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.StaffId),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("DepartmentId", user.DepartmentId),
                new Claim("TokenVersion", user.TokenVersion.ToString())
            }),
            Expires = DateTime.UtcNow.AddHours(8),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var handler = new JwtSecurityTokenHandler();
        return AuthServiceResult.SignedIn(handler.WriteToken(handler.CreateToken(descriptor)));
    }

    public async Task<AuthServiceResult> ApproveUserAsync(
        string staffId, string? actorStaffId, CancellationToken cancellationToken = default)
    {
        if (!TryNormalizeStaffId(staffId, out string target))
            return AuthServiceResult.Bad(InvalidStaffIdMessage);
        if (string.IsNullOrWhiteSpace(actorStaffId))
            return AuthServiceResult.Denied("Không xác định được Admin.");

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        int affectedRows = await _db.Users
            .Where(u => u.StaffId == target && u.Status == "PENDING_APPROVAL")
            .ExecuteUpdateAsync(setters => setters.SetProperty(u => u.Status, "ACTIVE"), cancellationToken);

        if (affectedRows != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            if (affectedRows > 1)
                return AuthServiceResult.ConflictResult("Có nhiều tài khoản trùng StaffId. Đã hủy thao tác.");

            bool exists = await _db.Users.AsNoTracking()
                .AnyAsync(u => u.StaffId == target, cancellationToken);
            return exists
                ? AuthServiceResult.Bad("Chỉ được duyệt tài khoản đang PENDING_APPROVAL.")
                : AuthServiceResult.Missing("Không tìm thấy nhân viên.");
        }

        AddSuccessAudit(actorStaffId, target, "APPROVE_USER");
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return AuthServiceResult.Ok("Đã duyệt nhân viên và ghi nhật ký bảo mật.", "ACTIVE");
    }

    public async Task<AuthServiceResult> RevokeSessionsAsync(
        string staffId, string? actorStaffId, CancellationToken cancellationToken = default)
    {
        if (!TryNormalizeStaffId(staffId, out string target))
            return AuthServiceResult.Bad(InvalidStaffIdMessage);
        if (string.IsNullOrWhiteSpace(actorStaffId))
            return AuthServiceResult.Denied("Không xác định được người thực hiện.");

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        int affectedRows = await _db.Users
            .Where(u => u.StaffId == target && u.TokenVersion < int.MaxValue)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(u => u.TokenVersion, u => u.TokenVersion + 1), cancellationToken);

        if (affectedRows != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            if (affectedRows > 1)
                return AuthServiceResult.ConflictResult("Có nhiều tài khoản trùng StaffId. Đã hủy thao tác.");
            bool exists = await _db.Users.AsNoTracking()
                .AnyAsync(u => u.StaffId == target, cancellationToken);
            return exists
                ? AuthServiceResult.ConflictResult("Không thể tăng TokenVersion.")
                : AuthServiceResult.Missing("Không tìm thấy nhân viên.");
        }

        AddSuccessAudit(actorStaffId, target, "REVOKE_SESSIONS");
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return AuthServiceResult.Ok("Đã thu hồi JWT và ghi nhật ký bảo mật.");
    }

    public async Task<AuthServiceResult> SuspendUserAsync(
        string staffId, string? actorStaffId, CancellationToken cancellationToken = default)
    {
        if (!TryNormalizeStaffId(staffId, out string target))
            return AuthServiceResult.Bad(InvalidStaffIdMessage);
        if (string.IsNullOrWhiteSpace(actorStaffId))
            return AuthServiceResult.Denied("Không xác định được Admin.");
        if (string.Equals(actorStaffId, target, StringComparison.OrdinalIgnoreCase))
            return AuthServiceResult.ConflictResult("Admin không thể tự khóa tài khoản.");

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        int affectedRows = await _db.Users
            .Where(u => u.StaffId == target && u.Role != "Admin" &&
                        u.Status == "ACTIVE" && u.TokenVersion < int.MaxValue)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(u => u.Status, "SUSPENDED")
                .SetProperty(u => u.TokenVersion, u => u.TokenVersion + 1), cancellationToken);

        if (affectedRows != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            if (affectedRows > 1)
                return AuthServiceResult.ConflictResult("Phát hiện trùng StaffId. Đã hủy thao tác.");
            var user = await _db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.StaffId == target, cancellationToken);
            if (user is null)
                return AuthServiceResult.Missing("Không tìm thấy nhân viên.");
            if (user.Role == "Admin")
                return AuthServiceResult.ConflictResult("Không được khóa tài khoản Admin bằng API này.");
            if (user.Status != "ACTIVE")
                return AuthServiceResult.ConflictResult("Chỉ được khóa tài khoản đang ACTIVE.");
            return AuthServiceResult.ConflictResult("Không thể khóa tài khoản hoặc tăng TokenVersion.");
        }

        AddSuccessAudit(actorStaffId, target, "SUSPEND_USER");
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return AuthServiceResult.Ok("Đã khóa tài khoản, thu hồi JWT và ghi nhật ký.", "SUSPENDED");
    }

    public async Task<AuthServiceResult> UnsuspendUserAsync(
        string staffId, string? actorStaffId, CancellationToken cancellationToken = default)
    {
        if (!TryNormalizeStaffId(staffId, out string target))
            return AuthServiceResult.Bad(InvalidStaffIdMessage);
        if (string.IsNullOrWhiteSpace(actorStaffId))
            return AuthServiceResult.Denied("Không xác định được Admin.");
        if (string.Equals(actorStaffId, target, StringComparison.OrdinalIgnoreCase))
            return AuthServiceResult.ConflictResult("Admin không thể tự mở khóa bằng API này.");

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        int affectedRows = await _db.Users
            .Where(u => u.StaffId == target && u.Role != "Admin" &&
                        u.Status == "SUSPENDED" && u.TokenVersion < int.MaxValue)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(u => u.Status, "ACTIVE")
                .SetProperty(u => u.TokenVersion, u => u.TokenVersion + 1), cancellationToken);

        if (affectedRows != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            if (affectedRows > 1)
                return AuthServiceResult.ConflictResult("Phát hiện trùng StaffId. Đã hủy thao tác.");
            var user = await _db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.StaffId == target, cancellationToken);
            if (user is null)
                return AuthServiceResult.Missing("Không tìm thấy nhân viên.");
            if (user.Role == "Admin")
                return AuthServiceResult.ConflictResult("Không được mở khóa tài khoản Admin bằng API này.");
            if (user.Status != "SUSPENDED")
                return AuthServiceResult.ConflictResult("Chỉ được mở khóa tài khoản đang SUSPENDED.");
            return AuthServiceResult.ConflictResult("Không thể mở khóa tài khoản hoặc tăng TokenVersion.");
        }

        AddSuccessAudit(actorStaffId, target, "UNSUSPEND_USER");
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return AuthServiceResult.Ok("Đã mở khóa tài khoản và ghi nhật ký bảo mật.", "ACTIVE");
    }

    private static bool TryNormalizeStaffId(string? value, out string normalized)
    {
        normalized = value?.Trim().ToUpperInvariant() ?? string.Empty;
        return normalized.Length <= 20 && StaffIdRegex.IsMatch(normalized);
    }

    private void AddSuccessAudit(string actorStaffId, string targetStaffId, string action)
    {
        _db.SecurityAuditLogs.Add(new SecurityAuditLog
        {
            ActorStaffId = actorStaffId,
            TargetStaffId = targetStaffId,
            Action = action,
            Result = "SUCCESS",
            CreatedAtUtc = DateTime.UtcNow
        });
    }
}
