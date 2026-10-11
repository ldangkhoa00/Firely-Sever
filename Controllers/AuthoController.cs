using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HospitalUsers.Data;
using HospitalUsers.Models;
using HospitalUsers.Security; // Nơi chứa PasswordHasher

namespace HospitalUsers.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        // Constructor nhận cả Database và File Cấu hình (để lấy mã bí mật JWT)
        public AuthController(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // --- DTO: Gói hàng Đăng Ký ---
        public class RegisterRequest
        {
            public string StaffId { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
            public string HospitalEmail { get; set; } = string.Empty;   
            public string PhoneNumber { get; set; } = string.Empty;
            public string DepartmentId { get; set; } = string.Empty;
            public string Role { get; set; } = "Nurse"; 
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterRequest request)
        {
            // 1. Kiểm tra xem mã nhân viên đã tồn tại chưa
            if (_context.Users.Any(u => u.StaffId == request.StaffId))
            {
                return Conflict(new { message = "Mã nhân viên đã được đăng ký." });
            }

            // 2. Băm mật khẩu
            string hashedPassword = PasswordHasher.HashPassword(request.Password);

            // 3. Tạo user mới với đầy đủ thông tin từ Form
            var newUser = new AppUser
            {
                StaffId = request.StaffId,
                PasswordHash = hashedPassword,
                FullName = request.FullName,
                PhoneNumber = request.PhoneNumber,
                DepartmentId = request.DepartmentId,
                Role = request.Role,
                Status = "PENDING_APPROVAL" // LUÔN LUÔN Ở TRẠNG THÁI CHỜ DUYỆT
            };

            // 4. Lưu vào CSDL
            _context.Users.Add(newUser);
            _context.SaveChanges();

            return Ok(new { message = "Đăng ký thành công! Vui lòng chờ phê duyệt." });
        }

        // --- DTO: Gói hàng Đăng Nhập ---
        public class LoginRequest
        {
            public string Identifier { get; set; } = string.Empty; // Có thể là StaffId
            public string Password { get; set; } = string.Empty;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            // 1. Tìm user trong CSDL
            var user = _context.Users.FirstOrDefault(u => u.StaffId == request.Identifier);

            // 2. Nếu không tìm thấy
            if (user == null)
            {
                return Unauthorized(new { message = "Mã nhân viên hoặc mật khẩu không chính xác." });
            }

            // 3. Kiểm tra mật khẩu
            if (!PasswordHasher.VerifyPassword(request.Password, user.PasswordHash))
            {
                return Unauthorized(new { message = "Mã nhân viên hoặc mật khẩu không chính xác." });
            }

            // 4. Kiểm tra trạng thái Zero-Trust (Nếu chưa được duyệt thì chặn lại)
            if (user.Status == "PENDING_APPROVAL")
            {
                return StatusCode(403, new { message = "Tài khoản của bạn đang chờ Quản trị viên duyệt." });
            }
            if (user.Status == "SUSPENDED")
            {
                return StatusCode(403, new { message = "Tài khoản đã bị tạm khóa." });
            }

            // 5. Mọi thứ OK -> Tạo JWT Token
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_config["JwtSettings:Key"] ?? "KhoaBaoMatCuaBenhVienRatDai123456789");
            
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.StaffId),
                    new Claim(ClaimTypes.Role, user.Role),
                    new Claim("DepartmentId", user.DepartmentId) // GHI CHÚ MÃ PHÒNG LÊN THẺ
                }),
                Expires = DateTime.UtcNow.AddHours(8), // Thẻ có hạn 8 tiếng (1 ca trực)
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            // 6. Trả thẻ về cho Giao diện
            return Ok(new { token = tokenString });
        }
        // --- API CHO QUẢN TRỊ VIÊN: DUYỆT TÀI KHOẢN ---
        // Đường dẫn của API này sẽ là: PUT api/Auth/approve/{staffId}
        [HttpPut("approve/{staffId}")]
        public IActionResult ApproveUser(string staffId)
        {
            // BƯỚC 1: Tìm nhân viên trong Database dựa vào Mã Nhân Viên (staffId) truyền vào từ URL
            var user = _context.Users.FirstOrDefault(u => u.StaffId == staffId);

            // BƯỚC 2: Kiểm tra xem nhân viên có tồn tại không
            if (user == null)
            {
                return NotFound(new { message = $"Không tìm thấy nhân viên với mã: {staffId}" });
            }

            // BƯỚC 3: Kiểm tra xem tài khoản này có đúng là đang cần duyệt không
            if (user.Status == "ACTIVE")
            {
                return BadRequest(new { message = "Tài khoản này đã được kích hoạt từ trước rồi!" });
            }

            if (user.Status == "SUSPENDED")
            {
                return BadRequest(new { message = "Không thể duyệt tài khoản đang bị khóa hình phạt. Hãy mở khóa trước." });
            }

            // BƯỚC 4: THỰC THI QUYỀN LỰC CỦA ADMIN - CẬP NHẬT TRẠNG THÁI
            // Đổi từ PENDING_APPROVAL sang ACTIVE
            user.Status = "ACTIVE";

            // BƯỚC 5: Lưu thay đổi vào SQL Server
            _context.SaveChanges();

            // Trả về thông báo thành công cho giao diện Admin
            return Ok(new 
            { 
                message = $"Đã duyệt thành công tài khoản cho nhân viên: {user.FullName}",
                newStatus = user.Status 
            });
        }
    }
}