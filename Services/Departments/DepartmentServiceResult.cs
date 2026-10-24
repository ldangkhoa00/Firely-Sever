namespace HospitalUsers.Services.Departments;

// Trạng thái nghiệp vụ, không phụ thuộc HTTP/MVC.
public enum DepartmentResultKind
{
    Ok,
    Created,
    BadRequest,
    Unauthorized,
    NotFound,
    Conflict
}

public sealed record DepartmentServiceResult(DepartmentResultKind Kind, object Body)
{
    public static DepartmentServiceResult Success(object body) =>
        new(DepartmentResultKind.Ok, body);

    public static DepartmentServiceResult Created(object body) =>
        new(DepartmentResultKind.Created, body);

    public static DepartmentServiceResult Bad(string message) =>
        new(DepartmentResultKind.BadRequest, new HospitalUsers.DTOs.Departments.DepartmentMessageResponse(message));

    public static DepartmentServiceResult Denied(string message) =>
        new(DepartmentResultKind.Unauthorized, new HospitalUsers.DTOs.Departments.DepartmentMessageResponse(message));

    public static DepartmentServiceResult Missing(string message) =>
        new(DepartmentResultKind.NotFound, new HospitalUsers.DTOs.Departments.DepartmentMessageResponse(message));

    public static DepartmentServiceResult Conflict(string message) =>
        new(DepartmentResultKind.Conflict, new HospitalUsers.DTOs.Departments.DepartmentMessageResponse(message));
}
