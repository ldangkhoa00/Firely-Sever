using System.ComponentModel.DataAnnotations;

namespace HospitalUsers.Models
{
    public class Patient
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string FullName { get; set; } = string.Empty;
        
        [Required]
        public string RoomNumber { get; set; } = string.Empty; 
    }
}