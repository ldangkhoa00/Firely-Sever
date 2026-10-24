namespace HospitalUsers.DTOs.Csi
{
    // Query parameters used by the CSI read service, not a database entity.
    public sealed class CsiRoomQueryDto
    {
        public const int DefaultLimit = 20;
        public const int MaxLimit = 100;
        public const int MaxRoomLength = 50;

        public CsiRoomQueryDto(string roomNumber, int limit = DefaultLimit)
        {
            RoomNumber = roomNumber;
            Limit = limit;
        }

        public string RoomNumber { get; }
        public int Limit { get; }
    }
}
