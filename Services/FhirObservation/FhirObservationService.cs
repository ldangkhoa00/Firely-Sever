using System.Text.RegularExpressions;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using HospitalUsers.DTOs.FhirObservation;

namespace HospitalUsers.Services.FhirObservation
{
    public sealed class FhirObservationService : IFhirObservationService
    {
        private static readonly Regex FhirPatientIdPattern = new(
            @"\A[A-Za-z0-9.-]+\z",
            RegexOptions.Compiled | RegexOptions.CultureInvariant,
            TimeSpan.FromSeconds(1));

        public FhirObservationResult ConvertCsi(CsiObservationInputDto input)
        {
            ArgumentNullException.ThrowIfNull(input);

            string patientId = input.PatientId?.Trim() ?? string.Empty;
            string csiData = input.CsiAmplitudeString?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(patientId))
            {
                return new(FhirObservationOutcome.MissingPatientId);
            }

            if (string.IsNullOrWhiteSpace(csiData))
            {
                return new(FhirObservationOutcome.MissingCsiData);
            }

            // Defense in depth when the service is called outside model binding.
            if (patientId.Length > CsiObservationInputDto.MaxPatientIdLength ||
                !FhirPatientIdPattern.IsMatch(patientId))
            {
                return new(FhirObservationOutcome.InvalidPatientId);
            }

            if (csiData.Length > CsiObservationInputDto.MaxCsiLength)
            {
                return new(FhirObservationOutcome.InvalidCsiLength);
            }

            // CSI is raw radio measurement; it is NOT a fall-detection diagnosis.
            // This uses an internal coding system, NOT an official LOINC code.
            var observation = new Observation
            {
                Id = Guid.NewGuid().ToString("D"),
                Status = ObservationStatus.Final,
                Code = new CodeableConcept(
                    "urn:hospitalusers:csi",
                    "CSI-WIFI",
                    "Raw WiFi Channel State Information"),
                Subject = new ResourceReference($"Patient/{patientId}"),
                Value = new FhirString(csiData),
                Effective = new FhirDateTime(DateTimeOffset.UtcNow)
            };

            string fhirJson = new FhirJsonSerializer()
                .SerializeToString(observation);

            // This conversion-only endpoint does NOT call AppDbContext or write to SQL.
            return new(FhirObservationOutcome.Success, fhirJson);
        }
    }
}
