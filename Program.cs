using HospitalUsers.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. Cấu hình kết nối SQL Server (Lấy từ appsettings.json)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Thiết lập Bảo vệ bằng JWT (Phân quyền Bác sĩ, Y tá, IT)
var jwtKey = builder.Configuration["JwtSettings:Key"] ?? "KhoaBaoMatCuaBenhVienRatDai123456789";
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
    });

builder.Services.AddControllers();
var app = builder.Build();

// 3. Kích hoạt cửa an ninh
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();