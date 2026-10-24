namespace HospitalUsers.DTOs.Departments;

public sealed record ActiveDepartmentResponse(string DepartmentId, string DepartmentName);

public sealed record DepartmentMessageResponse(string Message);

public sealed record DepartmentCreatedResponse(
    string Message,
    string DepartmentId,
    string DepartmentName,
    bool IsActive);

public sealed record DepartmentStatusResponse(
    string Message,
    string DepartmentId,
    bool IsActive);
