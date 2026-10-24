using HospitalUsers.Models;

namespace HospitalUsers.Services.Csi
{
    public enum CsiRoomResultStatus
    {
        Success,
        InvalidRoom,
        InvalidLimit,
        Forbidden
    }

    public sealed class CsiRoomResult
    {
        private CsiRoomResult(
            CsiRoomResultStatus status,
            IReadOnlyList<CsiRecord>? records = null)
        {
            Status = status;
            Records = records ?? Array.Empty<CsiRecord>();
        }

        public CsiRoomResultStatus Status { get; }
        public IReadOnlyList<CsiRecord> Records { get; }

        public static CsiRoomResult Succeeded(IReadOnlyList<CsiRecord> records) =>
            new(CsiRoomResultStatus.Success, records);

        public static CsiRoomResult InvalidRoom() =>
            new(CsiRoomResultStatus.InvalidRoom);

        public static CsiRoomResult InvalidLimit() =>
            new(CsiRoomResultStatus.InvalidLimit);

        public static CsiRoomResult AccessDenied() =>
            new(CsiRoomResultStatus.Forbidden);
    }
}
