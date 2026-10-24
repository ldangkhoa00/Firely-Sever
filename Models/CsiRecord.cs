using System;
using System.ComponentModel.DataAnnotations;

namespace HospitalUsers.Models
{
    /// <summary>
    /// Luu mot ban ghi CSI va noi dung FHIR Observation duoc tao tu du lieu thiet bi.
    ///
    /// Chu y ve tuong thich co so du lieu:
    /// - Giu nguyen bon thuoc tinh va kieu du lieu hien tai.
    /// - Khong them Required, MaxLength, Column, Index hay gia tri mac dinh SQL.
    /// - Viec kiem tra ma phong, gioi han payload va FHIR JSON thuc hien tai API/service.
    /// </summary>
    public class CsiRecord
    {
        /// <summary>Khoa chinh cua ban ghi trong SQL Server.</summary>
        [Key]
        public int Id { get; set; }

        /// <summary>Ma phong noi thiet bi CSI gui du lieu.</summary>
        public string RoomNumber { get; set; } = string.Empty;

        /// <summary>
        /// Noi dung FHIR Observation duoi dang chuoi JSON.
        /// Model khong tu xac nhan JSON co hop le theo chuan FHIR hay khong.
        /// </summary>
        public string FhirObservationJson { get; set; } = string.Empty;

        /// <summary>
        /// Thoi diem tao doi tuong theo UTC (khong phai default constraint cua SQL Server).
        /// </summary>
        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    }
}
