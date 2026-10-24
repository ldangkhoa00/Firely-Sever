using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using HospitalUsers.Data;
using HospitalUsers.DTOs.Iot;
using HospitalUsers.Models;
using Microsoft.EntityFrameworkCore;

namespace HospitalUsers.Services.Iot
{
    public sealed class IotCsiService : IIotCsiService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<IotCsiService> _logger;

        public IotCsiService(
            AppDbContext context,
            ILogger<IotCsiService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IotCsiResult> ReceiveAsync(
            EspCsiPayloadDto payload,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(payload);

            string room = payload.Room?.Trim() ?? string.Empty;
            string csiData = payload.CsiData?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(room))
            {
                return IotCsiResult.MissingRoom;
            }

            if (string.IsNullOrWhiteSpace(csiData))
            {
                return IotCsiResult.MissingCsiData;
            }

            // Defense in depth for calls bypassing Controller model validation.
            if (room.Length > EspCsiPayloadDto.MaxRoomLength)
            {
                return IotCsiResult.InvalidRoomLength;
            }

            if (csiData.Length > EspCsiPayloadDto.MaxCsiLength)
            {
                return IotCsiResult.InvalidCsiLength;
            }

            // Wi-Fi CSI is a raw measurement, NOT a validated fall diagnosis.
            // The coding system is internal; CSI-WIFI is not an official LOINC code.
            var observation = new Observation
            {
                Id = Guid.NewGuid().ToString("D"),
                Status = ObservationStatus.Final,
                Code = new CodeableConcept(
                    "urn:hospitalusers:csi",
                    "CSI-WIFI",
                    "Raw WiFi Channel State Information"),
                Effective = new FhirDateTime(DateTimeOffset.UtcNow),
                Value = new FhirString(csiData)
            };

            string fhirJson = new FhirJsonSerializer()
                .SerializeToString(observation);

            _context.CsiRecords.Add(new CsiRecord
            {
                RoomNumber = room,
                FhirObservationJson = fhirJson
            });

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex)
            {
                // Never log raw CSI, API keys or authentication headers.
                _logger.LogError(ex, "Khong the luu ban ghi CSI vao SQL Server.");
                return IotCsiResult.StorageFailed;
            }

            return IotCsiResult.Received;
        }
    }
}
