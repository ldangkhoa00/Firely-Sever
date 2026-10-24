using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Encodings.Web;

namespace HospitalUsers.Security
{
    /// <summary>
    /// Authenticates ESP32 requests using the shared 32-byte Base64 API key
    /// stored in DeviceAuth:ApiKey. Use only on endpoints explicitly requiring
    /// the DeviceApiKey scheme; employee endpoints continue to use JWT Bearer.
    /// </summary>
    public sealed class DeviceApiKeyAuthenticationHandler
        : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string SchemeName = "DeviceApiKey";
        public const string HeaderName = "X-Device-Api-Key";

        private const int KeySizeBytes = 32;
        private const int Base64KeyLength = 44; // Base64 of exactly 32 bytes

        private readonly IConfiguration _configuration;
        private readonly ILogger<DeviceApiKeyAuthenticationHandler> _logger;

        public DeviceApiKeyAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory loggerFactory,
            UrlEncoder encoder,
            IConfiguration configuration)
            : base(options, loggerFactory, encoder)
        {
            _configuration = configuration;
            _logger = loggerFactory.CreateLogger<DeviceApiKeyAuthenticationHandler>();
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // Fail closed if the server key is missing or malformed.
            // Never log or return the actual secret.
            string? configuredKey = _configuration["DeviceAuth:ApiKey"];

            if (!TryReadKey(configuredKey, out byte[] expectedKeyBytes))
            {
                _logger.LogError(
                    "DeviceAuth:ApiKey is missing or invalid. Expected a Base64-encoded 32-byte key.");

                return Failed();
            }

            // Reject absent, repeated, empty, oversized and malformed headers.
            if (!Request.Headers.TryGetValue(HeaderName, out var headerValues)
                || headerValues.Count != 1
                || !TryReadKey(headerValues[0], out byte[] providedKeyBytes))
            {
                return Failed();
            }

            // Both decoded values are exactly 32 bytes, making this a
            // constant-time comparison of the secret bytes.
            if (!CryptographicOperations.FixedTimeEquals(
                    expectedKeyBytes, providedKeyBytes))
            {
                return Failed();
            }

            // A shared key identifies a device *class*, not an individual ESP32.
            // Do not grant Doctor/Nurse/Admin role claims here.
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "ESP32-SHARED-KEY"),
                new Claim("PrincipalType", "Device")
            };

            var identity = new ClaimsIdentity(claims, SchemeName);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, SchemeName);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        private static Task<AuthenticateResult> Failed()
        {
            // Use one generic response for every invalid client credential.
            return Task.FromResult(AuthenticateResult.Fail(
                "Thong tin xac thuc thiet bi khong hop le."));
        }

        private static bool TryReadKey(string? base64Key, out byte[] keyBytes)
        {
            keyBytes = Array.Empty<byte>();

            // The project originally creates a random 32-byte secret encoded
            // with Convert.ToBase64String, which is exactly 44 characters.
            if (base64Key is null || base64Key.Length != Base64KeyLength)
            {
                return false;
            }

            byte[] decoded = new byte[KeySizeBytes];
            if (!Convert.TryFromBase64String(base64Key, decoded, out int written)
                || written != KeySizeBytes)
            {
                return false;
            }

            // Reject non-canonical Base64 spellings, including whitespace.
            if (!string.Equals(
                    Convert.ToBase64String(decoded),
                    base64Key,
                    StringComparison.Ordinal))
            {
                return false;
            }

            keyBytes = decoded;
            return true;
        }
    }
}
