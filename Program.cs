using HospitalUsers.Data;
using HospitalUsers.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// UDP LAN discovery: disabled by default until udp-discovery.json is configured.
builder.Configuration.AddJsonFile("udp-discovery.json", optional: true, reloadOnChange: false);
builder.Services.Configure<HospitalUsers.Services.Discovery.UdpDiscoveryOptions>(
    builder.Configuration.GetSection(HospitalUsers.Services.Discovery.UdpDiscoveryOptions.SectionName));
builder.Services.AddHostedService<HospitalUsers.Services.Discovery.UdpDiscoveryHostedService>();
builder.Services.AddScoped<HospitalUsers.Services.FhirObservation.IFhirObservationService, HospitalUsers.Services.FhirObservation.FhirObservationService>();
builder.Services.AddScoped<HospitalUsers.Services.Iot.IIotCsiService, HospitalUsers.Services.Iot.IotCsiService>();

// ============================================================
// 1. SQL SERVER
// ============================================================
string? connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Chua cau hinh ConnectionStrings:DefaultConnection.");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// ============================================================
// 2. AUTHENTICATION: JWT CHO NHAN VIEN + API KEY CHO ESP32
// ============================================================
string? jwtKey = builder.Configuration["JwtSettings:Key"];

if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException(
        "Chua cau hinh JwtSettings:Key. Server khong the khoi dong.");
}

byte[] jwtKeyBytes = Encoding.UTF8.GetBytes(jwtKey);

if (jwtKeyBytes.Length < 32)
{
    throw new InvalidOperationException(
        "JWT Secret phai co it nhat 32 byte UTF-8.");
}

// Bearer van la scheme MAC DINH cho cac endpoint nhan vien.
// DeviceApiKey chi duoc dung khi endpoint yeu cau scheme nay.
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(jwtKeyBytes),
            RequireSignedTokens = true,
            ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
            ValidateLifetime = true,
            RequireExpirationTime = true,
            ClockSkew = TimeSpan.FromMinutes(1),

            // GIU TUONG THICH voi token hien tai: AuthController chua
            // gan Issuer/Audience khi tao JWT. Can bo sung DONG BO
            // ben phat hanh token truoc khi bat hai kiem tra nay.
            ValidateIssuer = false,
            ValidateAudience = false,

            NameClaimType = ClaimTypes.NameIdentifier,
            RoleClaimType = ClaimTypes.Role
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var principal = context.Principal;

                string? staffId = principal?
                    .FindFirst(ClaimTypes.NameIdentifier)?.Value;
                string? tokenRole = principal?
                    .FindFirst(ClaimTypes.Role)?.Value;
                string? tokenDepartment = principal?
                    .FindFirst("DepartmentId")?.Value;
                string? tokenVersionText = principal?
                    .FindFirst("TokenVersion")?.Value;

                if (string.IsNullOrWhiteSpace(staffId) ||
                    string.IsNullOrWhiteSpace(tokenRole) ||
                    tokenDepartment == null ||
                    !int.TryParse(tokenVersionText, out int tokenVersion))
                {
                    context.Fail("JWT thieu hoac sai thong tin bat buoc.");
                    return;
                }

                // JWT hop le ve chu ky van phai duoc doi chieu voi
                // trang thai tai khoan, vai tro va TokenVersion trong DB.
                var db = context.HttpContext.RequestServices
                    .GetRequiredService<AppDbContext>();

                var user = await db.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        u => u.StaffId == staffId,
                        context.HttpContext.RequestAborted);

                if (user == null || user.Status != "ACTIVE")
                {
                    context.Fail("Tai khoan khong con hoat dong.");
                    return;
                }

                if (!string.Equals(user.Role, tokenRole,
                        StringComparison.Ordinal) ||
                    !string.Equals(user.DepartmentId, tokenDepartment,
                        StringComparison.Ordinal))
                {
                    context.Fail("Quyen truy cap da thay doi.");
                    return;
                }

                if (user.TokenVersion != tokenVersion)
                {
                    context.Fail("JWT da bi thu hoi hoac khong con hop le.");
                }
            }
        };
    })
    .AddScheme<AuthenticationSchemeOptions,
        DeviceApiKeyAuthenticationHandler>(
        DeviceApiKeyAuthenticationHandler.SchemeName,
        options => { });

// Dang ky phan quyen ro rang. Khong bat fallback policy de khong
// thay doi cac endpoint [AllowAnonymous] cua du an hien tai.
builder.Services.AddAuthorization();
builder.Services.AddControllers();
// Security Audit Logs: read-only filtering and pagination.
builder.Services.AddScoped<HospitalUsers.Services.SecurityAuditLogs.ISecurityAuditLogService, HospitalUsers.Services.SecurityAuditLogs.SecurityAuditLogService>();
// CSI room authorization and read-only business logic.
builder.Services.AddScoped<HospitalUsers.Services.Csi.ICsiService, HospitalUsers.Services.Csi.CsiService>();
// Departments business logic. DbContext lifetime is scoped.
builder.Services.AddScoped<HospitalUsers.Services.Departments.IDepartmentService, HospitalUsers.Services.Departments.DepartmentService>();
// Auth business logic is scoped to the HTTP request / DbContext.
builder.Services.AddScoped<HospitalUsers.Services.Auth.IAuthService, HospitalUsers.Services.Auth.AuthService>();

// ============================================================
// 3. RATE LIMITING: LOGIN, REGISTER, DEVICE CSI
// ============================================================
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode =
        StatusCodes.Status429TooManyRequests;

    // 10 yeu cau / 1 phut / IP
    options.AddPolicy("login", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString()
                ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    // 5 yeu cau / 10 phut / IP
    options.AddPolicy("register", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString()
                ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(10),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    // 60 yeu cau / 1 phut / IP - muc THU NGHIEM cho ESP32.
    // IoT va FhirObservation chia se cung policy/partition theo IP.
    options.AddPolicy("device-csi", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString()
                ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

// ============================================================
// 4. SWAGGER - CHI DEVELOPMENT
// ============================================================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ============================================================
// 5. HTTP PIPELINE
// ============================================================
app.UseRouting();

// Phai dat SAU UseRouting cho [EnableRateLimiting("...")].
// Giu truoc authentication de ca yeu cau thieu key bi tinh limit.
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
