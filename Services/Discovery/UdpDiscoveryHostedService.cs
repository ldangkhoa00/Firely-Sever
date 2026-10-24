using System.Globalization;
using System.Net;
using System.Net.Sockets;
using HospitalUsers.Services.Discovery;
using Microsoft.Extensions.Options;

namespace HospitalUsers.Services.Discovery;

public sealed class UdpDiscoveryHostedService : BackgroundService
{
    private readonly UdpDiscoveryOptions _options;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<UdpDiscoveryHostedService> _logger;

    public UdpDiscoveryHostedService(
        IOptions<UdpDiscoveryOptions> options,
        IHostEnvironment environment,
        ILogger<UdpDiscoveryHostedService> logger)
    {
        _options = options.Value;
        _environment = environment;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("UDP discovery disabled. Configure udp-discovery.json to enable it.");
            return;
        }

        // Fail closed on invalid configuration instead of advertising an unsafe endpoint.
        if (_options.Port is < 1024 or > 65535)
            throw new InvalidOperationException("UDP discovery port must be between 1024 and 65535.");

        if (!Uri.TryCreate(_options.AdvertisedApiUrl, UriKind.Absolute, out Uri? apiUrl) ||
            (apiUrl.Scheme != Uri.UriSchemeHttps && apiUrl.Scheme != Uri.UriSchemeHttp) ||
            apiUrl.HostNameType is UriHostNameType.Unknown or UriHostNameType.Basic ||
            apiUrl.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
            apiUrl.Host is "0.0.0.0" or "127.0.0.1" or "::1" ||
            !string.IsNullOrEmpty(apiUrl.UserInfo) ||
            !string.IsNullOrEmpty(apiUrl.Query) ||
            !string.IsNullOrEmpty(apiUrl.Fragment) ||
            apiUrl.AbsolutePath != "/")
            throw new InvalidOperationException(
                "UdpDiscovery:AdvertisedApiUrl must be an absolute LAN URL without secrets, query, or path.");

        if (!_environment.IsDevelopment() && apiUrl.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException(
                "UDP discovery must advertise HTTPS outside Development.");

        var permitted = ParseSubnets(_options.AllowedSubnets);
        if (permitted.Count == 0)
            throw new InvalidOperationException("UDP discovery requires AllowedSubnets.");

        // IPv4 discovery is intentional: LAN ESP32 clients use IPv4 broadcasts.
        using var listener = new UdpClient(new IPEndPoint(IPAddress.Any, _options.Port));
        listener.Client.ReceiveBufferSize = 8192;
        _logger.LogInformation("UDP discovery listening on UDP port {Port}. API advertised: {Url}",
            _options.Port, apiUrl.GetLeftPart(UriPartial.Authority));

        var recent = new Dictionary<IPAddress, DateTimeOffset>();
        while (!stoppingToken.IsCancellationRequested)
        {
            UdpReceiveResult received;
            try
            {
                received = await listener.ReceiveAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            IPAddress sender = received.RemoteEndPoint.Address;
            if (!IsLocalPrivateAddress(sender, _environment.IsDevelopment()) ||
                !permitted.Any(subnet => subnet.Contains(sender)))
                continue;

            if (!UdpDiscoveryProtocol.TryReadNonce(received.Buffer, out string nonce))
                continue;

            // Best-effort per-IP throttling; avoid unbounded memory use.
            DateTimeOffset now = DateTimeOffset.UtcNow;
            if (recent.TryGetValue(sender, out DateTimeOffset previous) &&
                now - previous < TimeSpan.FromMilliseconds(600))
                continue;

            if (recent.Count >= 1024) recent.Clear();
            recent[sender] = now;

            byte[] response = UdpDiscoveryProtocol.CreateResponse(
                nonce, apiUrl.GetLeftPart(UriPartial.Authority));
            if (response.Length > 512)
                throw new InvalidOperationException("UDP discovery response is unexpectedly large.");

            try
            {
                await listener.SendAsync(response.AsMemory(), received.RemoteEndPoint, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (SocketException ex)
            {
                // Do not log received packet contents or caller secrets.
                _logger.LogWarning(ex, "UDP discovery failed to reply to a LAN request.");
            }
        }
    }

    private static bool IsLocalPrivateAddress(IPAddress address, bool development)
    {
        if (address.AddressFamily != AddressFamily.InterNetwork) return false;
        if (IPAddress.IsLoopback(address)) return development;
        byte[] b = address.GetAddressBytes();
        return b[0] == 10 ||
               (b[0] == 172 && b[1] is >= 16 and <= 31) ||
               (b[0] == 192 && b[1] == 168);
    }

    private static List<Ipv4Subnet> ParseSubnets(string[]? values)
    {
        var subnets = new List<Ipv4Subnet>();
        foreach (string entry in values ?? Array.Empty<string>())
        {
            string[] parts = entry.Split('/');
            if (parts.Length != 2 ||
                !IPAddress.TryParse(parts[0], out IPAddress? network) ||
                network.AddressFamily != AddressFamily.InterNetwork ||
                !int.TryParse(parts[1], NumberStyles.None,
                    CultureInfo.InvariantCulture, out int prefix) ||
                prefix is < 16 or > 32)
            {
                throw new InvalidOperationException(
                    "Invalid UdpDiscovery:AllowedSubnets entry. Use private IPv4 CIDR /16 to /32.");
            }

            // Do not allow broad/public CIDRs, even if their prefix is syntactically valid.
            if (!IsLocalPrivateAddress(network, development: true))
                throw new InvalidOperationException(
                    "UdpDiscovery:AllowedSubnets may contain private IPv4 ranges or local loopback only.");

            subnets.Add(new Ipv4Subnet(network, prefix));
        }
        return subnets;
    }

    private sealed class Ipv4Subnet
    {
        private readonly uint _network;
        private readonly uint _mask;

        public Ipv4Subnet(IPAddress address, int prefix)
        {
            _mask = uint.MaxValue << (32 - prefix);
            _network = ToUInt32(address) & _mask;
        }

        public bool Contains(IPAddress address)
        {
            return address.AddressFamily == AddressFamily.InterNetwork &&
                   (ToUInt32(address) & _mask) == _network;
        }

        private static uint ToUInt32(IPAddress address)
        {
            byte[] a = address.GetAddressBytes();
            return ((uint)a[0] << 24) | ((uint)a[1] << 16) |
                   ((uint)a[2] << 8) | a[3];
        }
    }
}
