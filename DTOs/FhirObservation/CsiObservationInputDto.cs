using System.ComponentModel.DataAnnotations;

namespace HospitalUsers.DTOs.FhirObservation
{
    // Keep the existing JSON input fields: patientId and csiAmplitudeString.
    public sealed class CsiObservationInputDto
    {
        public const int MaxPatientIdLength = 64;
        public const int MaxCsiLength = 65536;

        [Required(ErrorMessage = "Mã bệnh nhân không được để trống.")]
        [MaxLength(MaxPatientIdLength, ErrorMessage = "Mã bệnh nhân không được vượt quá 64 ký tự.")]
        [RegularExpression(@"^[A-Za-z0-9.-]+$",
            ErrorMessage = "Mã bệnh nhân chỉ được chứa chữ cái, số, dấu chấm và dấu gạch ngang.")]
        public string PatientId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Dữ liệu CSI không được để trống.")]
        [MaxLength(MaxCsiLength, ErrorMessage = "Dữ liệu CSI vượt quá kích thước cho phép.")]
        public string CsiAmplitudeString { get; set; } = string.Empty;
    }
}
