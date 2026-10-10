using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HospitalUsers.Data;
using System.Security.Claims;

namespace HospitalUsers.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Doctor,Nurse")] // Chặn đứng IT
    public class CsiController : ControllerBase
    {
        private readonly AppDbContext _context;
        public CsiController(AppDbContext context) { _context = context; }

        [HttpGet("room/{roomNumber}")]
        public IActionResult GetCsi(string roomNumber)
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            
            // Nếu là Y tá, bắt buộc kiểm tra xem có lấy đúng phòng được phân công không
            if (role == "Nurse")
            {
                var allowedRoom = User.FindFirst("ManagedRoom")?.Value;
                if (allowedRoom != roomNumber) return Forbid(); // Phạt thẻ đỏ 403
            }

            var records = _context.CsiRecords.Where(r => r.RoomNumber == roomNumber).ToList();
            return Ok(records);
        }
    }
}