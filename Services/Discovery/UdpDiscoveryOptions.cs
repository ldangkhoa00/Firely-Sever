namespace HospitalUsers.Services.Discovery;

public sealed class UdpDiscoveryOptions
{
    public const string SectionName = "UdpDiscovery";

    // Disabled by default until an explicit LAN configuration is provided.
    public bool Enabled { get; set; } = false;
    public int Port { get; set; } = 37020;
    public string AdvertisedApiUrl { get; set; } = string.Empty;
    public string[] AllowedSubnets { get; set; } = Array.Empty<string>();
}
