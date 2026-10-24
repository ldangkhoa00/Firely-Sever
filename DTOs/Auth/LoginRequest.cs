namespace HospitalUsers.DTOs.Auth;

/// <summary>Giu nguyen cac ten truong JSON Identifier va Password.</summary>
public sealed class LoginRequest
{
    public string Identifier { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
