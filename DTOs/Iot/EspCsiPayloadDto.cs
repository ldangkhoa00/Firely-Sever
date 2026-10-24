using System.ComponentModel.DataAnnotations;

namespace HospitalUsers.DTOs.Iot
{
    // ESP32 request JSON remains: { "room": "...", "csiData": "..." }.
    public sealed class EspCsiPayloadDto
    {
        public const int MaxRoomLength = 50;
        public const int MaxCsiLength = 65536;

        [Required(ErrorMessage = "Mã phòng không được để trống.")]
        [MaxLength(MaxRoomLength,
            ErrorMessage = "Mã phòng không được vượt quá 50 ký tự.")]
        public string Room { get; set; } = string.Empty;

        [Required(ErrorMessage = "Dữ liệu CSI không được để trống.")]
        [MaxLength(MaxCsiLength,
            ErrorMessage = "Dữ liệu CSI vượt quá kích thước cho phép.")]
        public string CsiData { get; set; } = string.Empty;
    }
}
