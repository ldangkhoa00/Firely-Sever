namespace HospitalUsers.Services.Iot
{
    // Domain-level outcomes: HTTP status codes belong in the Controller.
    public enum IotCsiResult
    {
        Received,
        MissingRoom,
        MissingCsiData,
        InvalidRoomLength,
        InvalidCsiLength,
        StorageFailed
    }
}
