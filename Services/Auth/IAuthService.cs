using HospitalUsers.DTOs.Auth;

namespace HospitalUsers.Services.Auth;

public interface IAuthService
{
    Task<AuthServiceResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthServiceResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<AuthServiceResult> ApproveUserAsync(string staffId, string? actorStaffId, CancellationToken cancellationToken = default);
    Task<AuthServiceResult> RevokeSessionsAsync(string staffId, string? actorStaffId, CancellationToken cancellationToken = default);
    Task<AuthServiceResult> SuspendUserAsync(string staffId, string? actorStaffId, CancellationToken cancellationToken = default);
    Task<AuthServiceResult> UnsuspendUserAsync(string staffId, string? actorStaffId, CancellationToken cancellationToken = default);
}
