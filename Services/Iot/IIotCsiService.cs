using HospitalUsers.DTOs.Iot;

namespace HospitalUsers.Services.Iot
{
    public interface IIotCsiService
    {
        Task<IotCsiResult> ReceiveAsync(
            EspCsiPayloadDto payload,
            CancellationToken cancellationToken = default);
    }
}
