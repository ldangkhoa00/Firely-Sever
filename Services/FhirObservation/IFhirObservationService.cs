using HospitalUsers.DTOs.FhirObservation;

namespace HospitalUsers.Services.FhirObservation
{
    public interface IFhirObservationService
    {
        FhirObservationResult ConvertCsi(CsiObservationInputDto input);
    }
}
