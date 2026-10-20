using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Data.SqlClient;
using System.Text;
using HospitalUsers.Data;
using System.ComponentModel.DataAnnotations;
using HospitalUsers.Models;
using Microsoft.AspNetCore.Authorization;
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
            
            
            
            [Required(ErrorMessage = "Mã nhân viên không được để trống.")]
            [MaxLength(20, ErrorMessage = "Mã nhân viên không được vượt quá 20 ký tự.")]
            [RegularExpression(
                @"\ABV-[A-Z0-9]+(-[A-Z0-9]+)*\z",
                ErrorMessage = "Mã nhân viên phải bắt đầu bằng BV- và chỉ chứa chữ IN HOA, số hoặc dấu gạch ngang giữa các nhóm."
            )]
            public string StaffId { get; set; } = string.Empty;



            
            [Required(ErrorMessage = "Mật khẩu không được để trống.")]
            [MinLength(12, ErrorMessage = "Mật khẩu phải có ít nhất 12 ký tự.")]
            public string Password { get; set; } = string.Empty;

            
           
            [Required(ErrorMessage = "Họ tên nhân viên không được để trống.")]
            [MaxLength(100, ErrorMessage = "Họ tên nhân viên không được vượt quá 100 ký tự.")]
            public string FullName { get; set; } = string.Empty;


            
            [Required(ErrorMessage = "Email bệnh viện không được để trống.")]
            [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
            [MaxLength(254, ErrorMessage = "Email không được vượt quá 254 ký tự.")]
            [RegularExpression(
                @"^[^@\s]+@hospital\.example$",
                ErrorMessage = "Email phải thuộc tên miền @hospital.example."
            )]
        public string HospitalEmail { get; set; } = string.Empty;
  
            
            
            [Required(ErrorMessage = "Số điện thoại không được để trống.")]
            [MaxLength(10, ErrorMessage = "Số điện thoại không được vượt quá 10 ký tự.")]
            [RegularExpression(
                @"^0[35789][0-9]{8}$",
                ErrorMessage = "Số di động phải có đúng 10 chữ số và bắt đầu bằng 03, 05, 07, 08 hoặc 09."
            )]
            public string PhoneNumber { get; set; } = string.Empty;


            
            [Required(ErrorMessage = "Mã khoa/phòng không được để trống.")]
            [MaxLength(50, ErrorMessage = "Mã khoa/phòng không được vượt quá 50 ký tự.")]
            public string DepartmentId { get; set; } = string.Empty;

            public string Role { get; set; } = "Nurse"; 
        }

        [HttpPost("register")]
        [EnableRateLimiting("register")]
        public IActionResult Register([FromBody] RegisterRequest request)
        {
            
            // BẢO MẬT: Chỉ cho phép đăng ký Nurse hoặc Doctor.
            // Chuẩn hóa chữ hoa/chữ thường trước khi lưu.

            string? normalizedRole = request.Role?.Trim().ToLowerInvariant() switch
            {
                "nurse" => "Nurse",
                "doctor" => "Doctor",
                _ => null
            };

            if (normalizedRole == null)
            {
                return BadRequest(new
                {
                    message = "Vai trò đăng ký không hợp lệ. Chỉ được đăng ký Nurse hoặc Doctor."
                });
            }
            
            // Chuẩn hóa mã khoa/phòng
            string normalizedDepartmentId =
                request.DepartmentId.Trim().ToUpperInvariant();

            // Kiểm tra khoa/phòng có tồn tại và đang hoạt động không
            bool validDepartment = _context.Departments.Any(d =>
                d.DepartmentId == normalizedDepartmentId && d.IsActive);

            if (!validDepartment)
            {
                return BadRequest(new
                {
                    message = "Mã khoa/phòng không tồn tại hoặc đã ngừng hoạt động."
                });
            }

            string normalizedHospitalEmail =
            request.HospitalEmail.Trim().ToLowerInvariant();


            
            // 0. Kiểm tra email bệnh viện đã được đăng ký chưa
            if (_context.Users.Any(u =>
                u.HospitalEmail == normalizedHospitalEmail))
            {
                return Conflict(new
                {
                    message = "Email bệnh viện đã được đăng ký."
                });
            }


            // 1. Kiểm tra xem mã nhân viên đã tồn tại chưa
            if (_context.Users.Any(u => u.StaffId == request.StaffId))
            {
                return Conflict(new { message = "Mã nhân viên đã được đăng ký." });
            }


            // BẢO MẬT: Giới hạn mật khẩu tối đa 72 byte UTF-8
            if (System.Text.Encoding.UTF8.GetByteCount(request.Password) > 72)
            {
                return BadRequest(new
                {
                    message = "Mật khẩu không được vượt quá 72 byte UTF-8."
                });
            }

            // Băm mật khẩu bằng BCrypt


            // 2. Băm mật khẩu
            string hashedPassword = PasswordHasher.HashPassword(request.Password);

            // 3. Tạo user mới với đầy đủ thông tin từ Form
            var newUser = new AppUser
            {
                StaffId = request.StaffId,
                PasswordHash = hashedPassword,
                FullName = request.FullName,
                HospitalEmail = normalizedHospitalEmail,
                PhoneNumber = request.PhoneNumber,
                DepartmentId = normalizedDepartmentId,
                Role = normalizedRole,
                Status = "PENDING_APPROVAL" // LUÔN LUÔN Ở TRẠNG THÁI CHỜ DUYỆT
            };

            // 4. Lưu vào CSDL
            _context.Users.Add(newUser);

            try
            {
                _context.SaveChanges();
            }
            
        catch (DbUpdateException ex) when (
            ex.InnerException is SqlException sqlEx &&
            (sqlEx.Number == 2601 || sqlEx.Number == 2627))
        {
            // Kiểm tra mã nhân viên có bị trùng không
            if (_context.Users
                .AsNoTracking()
                .Any(u => u.StaffId == request.StaffId))
            {
                return Conflict(new
                {
                    message = "Mã nhân viên đã được đăng ký."
                });
            }

            // Kiểm tra email bệnh viện có bị trùng không
            if (_context.Users
                .AsNoTracking()
                .Any(u => u.HospitalEmail == normalizedHospitalEmail))
            {
                return Conflict(new
                {
                    message = "Email bệnh viện đã được đăng ký."
                });
            }

            // Nếu không xác định được lỗi trùng ở hai trường trên,
            // không che giấu lỗi database.
            throw;
        }
        return Ok(new
        {
            message = "Đăng ký thành công! Vui lòng chờ phê duyệt."
        });

        }

        // --- DTO: Gói hàng Đăng Nhập ---
        public class LoginRequest
        {
            public string Identifier { get; set; } = string.Empty; // Có thể là StaffId
            public string Password { get; set; } = string.Empty;
        }

        
        [HttpPost("login")]
        [EnableRateLimiting("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {

            // BẢO MẬT: Không cho BCrypt xử lý mật khẩu vượt quá 72 byte UTF-8
            if (string.IsNullOrEmpty(request.Password) ||
                Encoding.UTF8.GetByteCount(request.Password) > 72)
            {
                return Unauthorized(new
                {
                    message = "Mã nhân viên hoặc mật khẩu không chính xác."
                });
            }

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

            // Tài khoản đang chờ Admin duyệt
            if (user.Status == "PENDING_APPROVAL")
            {
                return StatusCode(403, new
                {
                    message = "Tài khoản của bạn đang chờ Quản trị viên duyệt."
                });
            }

            // Tài khoản đã bị khóa
            if (user.Status == "SUSPENDED")
            {
                return StatusCode(403, new
                {
                    message = "Tài khoản đã bị tạm khóa."
                });
            }

            // BẢO MẬT: Mọi trạng thái khác ACTIVE đều bị từ chối
            if (user.Status != "ACTIVE")
            {
                return StatusCode(403, new
                {
                    message = "Trạng thái tài khoản không hợp lệ hoặc chưa được kích hoạt. Vui lòng liên hệ quản trị viên."
                });
            }


            // 5. Mọi thứ OK -> Tạo JWT Token
            
            var tokenHandler = new JwtSecurityTokenHandler();

            var configuredKey = _config["JwtSettings:Key"]
                ?? throw new InvalidOperationException(
                    "Chua cau hinh JwtSettings:Key."
                );

            var key = Encoding.UTF8.GetBytes(configuredKey);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.StaffId),
                    new Claim(ClaimTypes.Role, user.Role),
                    new Claim("DepartmentId", user.DepartmentId), // GHI CHÚ MÃ PHÒNG LÊN THẺ
                    new Claim("TokenVersion", user.TokenVersion.ToString())
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
        
        [Authorize(Roles = "Admin")]
        [HttpPut("approve/{staffId}")]
        public async Task<IActionResult> ApproveUser(string staffId)
        {
            // 1. Kiểm tra mã nhân viên
            if (string.IsNullOrWhiteSpace(staffId))
            {
                return BadRequest(new
                {
                    message = "Mã nhân viên không hợp lệ."
                });
            }

            // 2. Lấy danh tính Admin từ JWT
            var actorStaffId = User
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(actorStaffId))
            {
                return Unauthorized(new
                {
                    message = "Không xác định được Admin."
                });
            }

            // 3. Bắt đầu giao dịch database
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            // 4. Chỉ duyệt tài khoản đang PENDING_APPROVAL
            int affectedRows = await _context.Users
                .Where(u => u.StaffId == staffId
                        && u.Status == "PENDING_APPROVAL")
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(u => u.Status, "ACTIVE")
                );

            // 5. Nếu không cập nhật đúng một tài khoản
            if (affectedRows != 1)
            {
                await transaction.RollbackAsync();

                if (affectedRows > 1)
                {
                    return Conflict(new
                    {
                        message = "Có nhiều tài khoản trùng StaffId. Đã hủy thao tác."
                    });
                }

                bool exists = await _context.Users
                    .AnyAsync(u => u.StaffId == staffId);

                if (!exists)
                {
                    return NotFound(new
                    {
                        message = "Không tìm thấy nhân viên."
                    });
                }

                return BadRequest(new
                {
                    message = "Chỉ được duyệt tài khoản đang PENDING_APPROVAL."
                });
            }

            // 6. Tạo nhật ký phê duyệt
            var auditLog = new SecurityAuditLog
            {
                ActorStaffId = actorStaffId,
                TargetStaffId = staffId,
                Action = "APPROVE_USER",
                Result = "SUCCESS",
                CreatedAtUtc = DateTime.UtcNow
            };

            // 7. Ghi nhật ký vào SQL Server
            _context.SecurityAuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            // 8. Xác nhận cả hai thao tác thành công
            await transaction.CommitAsync();

            return Ok(new
            {
                message = "Đã duyệt nhân viên và ghi nhật ký bảo mật.",
                newStatus = "ACTIVE"
            });
        }

                
                
        [Authorize(Roles = "Admin")]
        [HttpPut("revoke-sessions/{staffId}")]
        public async Task<IActionResult> RevokeSessions(string staffId)
        {
            // 1. Kiểm tra mã nhân viên cần thu hồi
            if (string.IsNullOrWhiteSpace(staffId))
            {
                return BadRequest(new
                {
                    message = "Mã nhân viên không hợp lệ."
                });
            }

            // 2. Xác định Admin đang thực hiện thao tác
            // Lấy từ JWT đã được xác thực, không lấy từ dữ liệu người dùng tự nhập
            var actorStaffId = User
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(actorStaffId))
            {
                return Unauthorized(new
                {
                    message = "Không xác định được người thực hiện."
                });
            }

            // 3. Bắt đầu giao dịch SQL Server
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            // 4. Thu hồi JWT bằng cách tăng TokenVersion
            int affectedRows = await _context.Users
                .Where(u => u.StaffId == staffId
                        && u.TokenVersion < int.MaxValue)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(
                        u => u.TokenVersion,
                        u => u.TokenVersion + 1
                    )
                );

            // 5. Phải cập nhật đúng một tài khoản
            if (affectedRows != 1)
            {
                await transaction.RollbackAsync();

                if (affectedRows > 1)
                {
                    return Conflict(new
                    {
                        message = "Có nhiều tài khoản trùng StaffId. Đã hủy thao tác."
                    });
                }

                bool exists = await _context.Users
                    .AnyAsync(u => u.StaffId == staffId);

                if (!exists)
                {
                    return NotFound(new
                    {
                        message = "Không tìm thấy nhân viên."
                    });
                }

                return Conflict(new
                {
                    message = "Không thể tăng TokenVersion."
                });
            }

            // 6. Tạo bản ghi nhật ký bảo mật
            var auditLog = new SecurityAuditLog
            {
                ActorStaffId = actorStaffId,
                TargetStaffId = staffId,
                Action = "REVOKE_SESSIONS",
                Result = "SUCCESS",
                CreatedAtUtc = DateTime.UtcNow
            };

            // 7. Đưa bản ghi vào EF Core và lưu SQL Server
            _context.SecurityAuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            // 8. Chỉ hoàn tất khi cả cập nhật và ghi nhật ký thành công
            await transaction.CommitAsync();

            return Ok(new
            {
                message = "Đã thu hồi JWT và ghi nhật ký bảo mật."
            });
        }
                
        [Authorize(Roles = "Admin")]
        [HttpPut("suspend/{staffId}")]
        public async Task<IActionResult> SuspendUser(string staffId)
        {
            // 1. Kiểm tra mã nhân viên
            if (string.IsNullOrWhiteSpace(staffId) || staffId.Length > 20)
            {
                return BadRequest(new
                {
                    message = "Mã nhân viên không hợp lệ."
                });
            }

            // 2. Xác định Admin thực hiện thao tác
            var actorStaffId = User
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(actorStaffId))
            {
                return Unauthorized(new
                {
                    message = "Không xác định được Admin."
                });
            }

            // 3. Không cho Admin tự khóa chính mình
            if (string.Equals(actorStaffId, staffId,
                StringComparison.OrdinalIgnoreCase))
            {
                return Conflict(new
                {
                    message = "Admin không thể tự khóa tài khoản."
                });
            }

            // 4. Bắt đầu giao dịch SQL Server
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            // 5. Chỉ khóa tài khoản ACTIVE không thuộc quyền Admin
            // Đồng thời tăng TokenVersion để thu hồi JWT cũ
            int affectedRows = await _context.Users
                .Where(u => u.StaffId == staffId
                        && u.Role != "Admin"
                        && u.Status == "ACTIVE"
                        && u.TokenVersion < int.MaxValue)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(u => u.Status, "SUSPENDED")
                    .SetProperty(
                        u => u.TokenVersion,
                        u => u.TokenVersion + 1
                    )
                );

        // 6. Yêu cầu cập nhật chính xác một tài khoản
        if (affectedRows != 1)
        {
            await transaction.RollbackAsync();

            if (affectedRows > 1)
            {
                return Conflict(new
                {
                    message = "Phát hiện trùng StaffId. Đã hủy thao tác."
                });
            }

            var target = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.StaffId == staffId);

            if (target == null)
            {
                return NotFound(new
                {
                    message = "Không tìm thấy nhân viên."
                });
            }

            if (target.Role == "Admin")
            {
                return Conflict(new
                {
                    message = "Không được khóa tài khoản Admin bằng API này."
                });
            }

            if (target.Status != "ACTIVE")
            {
                return Conflict(new
                {
                    message = "Chỉ được khóa tài khoản đang ACTIVE."
                });
            }

            return Conflict(new
            {
                message = "Không thể khóa tài khoản hoặc tăng TokenVersion."
            });
        }

                // 7. Tạo nhật ký bảo mật
                var auditLog = new SecurityAuditLog
                {
                    ActorStaffId = actorStaffId,
                    TargetStaffId = staffId,
                    Action = "SUSPEND_USER",
                    Result = "SUCCESS",
                    CreatedAtUtc = DateTime.UtcNow
                };

                // 8. Lưu nhật ký
                _context.SecurityAuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();

                // 9. Xác nhận giao dịch khi tất cả thành công
                await transaction.CommitAsync();

                return Ok(new
                {
                    message = "Đã khóa tài khoản, thu hồi JWT và ghi nhật ký.",
                    newStatus = "SUSPENDED"
                });
            }
        
        [Authorize(Roles = "Admin")]
        [HttpPut("unsuspend/{staffId}")]
        public async Task<IActionResult> UnsuspendUser(string staffId)
        {
            // 1. Kiểm tra mã nhân viên
            if (string.IsNullOrWhiteSpace(staffId) || staffId.Length > 20)
            {
                return BadRequest(new
                {
                    message = "Mã nhân viên không hợp lệ."
                });
            }

            // 2. Xác định Admin thực hiện thao tác
            var actorStaffId = User
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(actorStaffId))
            {
                return Unauthorized(new
                {
                    message = "Không xác định được Admin."
                });
            }

            // 3. Không cho Admin thao tác trên chính mình
            if (string.Equals(
                actorStaffId,
                staffId,
                StringComparison.OrdinalIgnoreCase))
            {
                return Conflict(new
                {
                    message = "Admin không thể tự mở khóa bằng API này."
                });
            }

            // 4. Bắt đầu Database Transaction
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            // 5. Chỉ mở khóa tài khoản Nurse/Doctor đang SUSPENDED
            // Đồng thời tăng TokenVersion
            int affectedRows = await _context.Users
                .Where(u => u.StaffId == staffId
                        && (u.Role == "Nurse" || u.Role == "Doctor")
                        && u.Status == "SUSPENDED"
                        && u.TokenVersion < int.MaxValue)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(u => u.Status, "ACTIVE")
                    .SetProperty(
                        u => u.TokenVersion,
                        u => u.TokenVersion + 1
                    )
                );

            // 6. Phải cập nhật chính xác một tài khoản
            if (affectedRows != 1)
            {
                await transaction.RollbackAsync();

                if (affectedRows > 1)
                {
                    return Conflict(new
                    {
                        message = "Phát hiện trùng StaffId. Đã hủy thao tác."
                    });
                }

                var target = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.StaffId == staffId);

                if (target == null)
                {
                    return NotFound(new
                    {
                        message = "Không tìm thấy nhân viên."
                    });
                }

                if (target.Role == "Admin")
                {
                    return Conflict(new
                    {
                        message = "Không được mở khóa tài khoản Admin bằng API này."
                    });
                }

                if (target.Status != "SUSPENDED")
                {
                    return Conflict(new
                    {
                        message = "Chỉ được mở khóa tài khoản đang SUSPENDED."
                    });
                }

                return Conflict(new
                {
                    message = "Không thể mở khóa tài khoản hoặc tăng TokenVersion."
                });
            }

            // 7. Ghi nhật ký bảo mật
            _context.SecurityAuditLogs.Add(new SecurityAuditLog
            {
                ActorStaffId = actorStaffId,
                TargetStaffId = staffId,
                Action = "UNSUSPEND_USER",
                Result = "SUCCESS",
                CreatedAtUtc = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            // 8. Xác nhận cả ba thay đổi
            await transaction.CommitAsync();

            return Ok(new
            {
                message = "Đã mở khóa tài khoản và ghi nhật ký bảo mật.",
                newStatus = "ACTIVE"
            });
        }

    }
}