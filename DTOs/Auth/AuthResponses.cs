using System.Text.Json.Serialization;

namespace HospitalUsers.DTOs.Auth;

// Giu nguyen response JSON: { "message": ... }, { "token": ... },
// { "message": ..., "newStatus": ... }. Khong tra PasswordHash.
public sealed record AuthMessageResponse(
    [property: JsonPropertyName("message")] string Message);

public sealed record AuthTokenResponse(
    [property: JsonPropertyName("token")] string Token);

public sealed record AuthStatusResponse(
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("newStatus")] string NewStatus);
