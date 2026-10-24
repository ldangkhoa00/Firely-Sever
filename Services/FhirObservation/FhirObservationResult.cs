namespace HospitalUsers.Services.FhirObservation
{
    public enum FhirObservationOutcome
    {
        Success,
        MissingPatientId,
        MissingCsiData,
        InvalidPatientId,
        InvalidCsiLength
    }

    // No HTTP status codes or EF entities in the service contract.
    public sealed record FhirObservationResult(
        FhirObservationOutcome Outcome,
        string? FhirJson = null);
}
