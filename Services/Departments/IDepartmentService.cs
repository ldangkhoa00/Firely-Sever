using HospitalUsers.DTOs.Departments;

namespace HospitalUsers.Services.Departments;

public interface IDepartmentService
{
    Task<IReadOnlyList<ActiveDepartmentResponse>> GetActiveAsync(CancellationToken cancellationToken = default);

    Task<DepartmentServiceResult> CreateAsync(
        CreateDepartmentDto request,
        string? actorStaffId,
        CancellationToken cancellationToken = default);

    Task<DepartmentServiceResult> DeactivateAsync(
        string departmentId,
        string? actorStaffId,
        CancellationToken cancellationToken = default);

    Task<DepartmentServiceResult> ActivateAsync(
        string departmentId,
        string? actorStaffId,
        CancellationToken cancellationToken = default);
}
