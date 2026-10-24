namespace HospitalUsers.Services.Auth;

// Ket qua nghiep vu: Service khong phu thuoc Controller/HTTP.
public enum AuthResultKind
{
    Success,
    BadRequest,
    Unauthorized,
    Forbidden,
    NotFound,
    Conflict
}

public sealed record AuthServiceResult(
    AuthResultKind Kind,
    string? Message = null,
    string? Token = null,
    string? NewStatus = null)
{
    public static AuthServiceResult Ok(string message, string? newStatus = null) =>
        new(AuthResultKind.Success, message, NewStatus: newStatus);

    public static AuthServiceResult SignedIn(string token) =>
        new(AuthResultKind.Success, Token: token);

    public static AuthServiceResult Bad(string message) =>
        new(AuthResultKind.BadRequest, message);

    public static AuthServiceResult Denied(string message) =>
        new(AuthResultKind.Unauthorized, message);

    public static AuthServiceResult ForbiddenResult(string message) =>
        new(AuthResultKind.Forbidden, message);

    public static AuthServiceResult Missing(string message) =>
        new(AuthResultKind.NotFound, message);

    public static AuthServiceResult ConflictResult(string message) =>
        new(AuthResultKind.Conflict, message);
}
