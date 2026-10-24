namespace HospitalUsers.DTOs.FhirObservation
{
    // ASP.NET Core's existing web JSON naming policy serializes these as message/data.
    // Data is a JSON-encoded FHIR resource string, not a nested JSON object.
    public sealed class FhirObservationResponseDto
    {
        public string Message { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty;
    }
}
