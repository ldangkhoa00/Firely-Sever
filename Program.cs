using HospitalUsers.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;


var builder = WebApplication.CreateBuilder(args);

// 1. Cấu hình kết nối SQL Server (Lấy từ appsettings.json)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Thiết lập Bảo vệ bằng JWT (Phân quyền Bác sĩ, Y tá, IT)

var jwtKey = builder.Configuration["JwtSettings:Key"];

if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException(
        "Chua cau hinh JwtSettings:Key. Server khong the khoi dong."
    );
}

if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException(
        "JWT Secret phai co it nhat 32 byte UTF-8."
    );
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = false,
            ValidateAudience = false
        };

                
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                // 1. Lấy thông tin nhân viên từ JWT
                var staffId = context.Principal?
                    .FindFirst(ClaimTypes.NameIdentifier)?.Value;

                var tokenRole = context.Principal?
                    .FindFirst(ClaimTypes.Role)?.Value;

                var tokenDepartment = context.Principal?
                    .FindFirst("DepartmentId")?.Value;

                var tokenVersionText = context.Principal?
                    .FindFirst("TokenVersion")?.Value;
                


                // 2. Kiểm tra JWT có đủ thông tin cần thiết
                if (string.IsNullOrWhiteSpace(staffId) ||
                    string.IsNullOrWhiteSpace(tokenRole) ||
                    tokenDepartment == null)
                {
                    context.Fail("JWT thieu thong tin bat buoc.");
                    return;
                }

                // 3. Lấy DbContext từ Dependency Injection
                var db = context.HttpContext.RequestServices
                    .GetRequiredService<AppDbContext>();

                // 4. Tìm tài khoản hiện tại trong SQL Server
                var user = await db.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.StaffId == staffId);
                    

                // 5. Nếu tài khoản không tồn tại hoặc bị khóa
                if (user == null || user.Status != "ACTIVE")
                {
                    context.Fail("Tai khoan khong con hoat dong.");
                    return;
                }

                // 6. Nếu vai trò hoặc khoa/phòng đã thay đổi
                if (!string.Equals(user.Role, tokenRole,
                        StringComparison.Ordinal) ||
                    !string.Equals(user.DepartmentId, tokenDepartment,
                        StringComparison.Ordinal))
                {
                    context.Fail("Quyen truy cap da thay doi.");
                    return;
                }
                
                // 7. Kiểm tra phiên bản xác thực của JWT
                if (!int.TryParse(tokenVersionText, out int jwtVersion))
                {
                    context.Fail("JWT thieu TokenVersion hoac TokenVersion khong hop le.");
                    return;
                }

                // So sánh phiên bản JWT với phiên bản trong Database
                if (jwtVersion != user.TokenVersion)
                {
                    context.Fail("JWT da bi thu hoi hoac khong con hop le.");
                    return;
                }

            }
        };

    });

        builder.Services.AddControllers();

        // BẢO MẬT: Giới hạn số lần gọi API đăng nhập

        builder.Services.AddRateLimiter(options =>
        {
            // Trả HTTP 429 khi vượt giới hạn           
            options.RejectionStatusCode =
                StatusCodes.Status429TooManyRequests;

            // CHÍNH SÁCH 1: Đăng nhập
            // Tối đa 10 yêu cầu trong 1 phút trên mỗi IP
            options.AddPolicy("login", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    }));

            // CHÍNH SÁCH 2: Đăng ký
            // Tối đa 5 yêu cầu trong 10 phút trên mỗi IP
            options.AddPolicy("register", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(10),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    }));
    });


// [BỔ SUNG] 3. Đăng ký dịch vụ Swagger để tự động quét các API của bạn
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// [BỔ SUNG] 4. Kích hoạt giao diện web Swagger (chỉ chạy trong môi trường code/Development)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


// 5. Xác định API mà người dùng đang gọi
app.UseRouting();

// 6. Kích hoạt Rate Limiting
app.UseRateLimiter();

// 7. Kích hoạt xác thực JWT và phân quyền
app.UseAuthentication();
app.UseAuthorization();

// 8. Kết nối các API Controller
app.MapControllers();

app.Run();
