using HospitalUsers.DTOs.Csi;

namespace HospitalUsers.Services.Csi
{
    public interface ICsiService
    {
        Task<CsiRoomResult> GetRoomAsync(
            CsiRoomQueryDto query,
            string? staffId,
            bool isNurse,
            CancellationToken cancellationToken = default);
    }
}
