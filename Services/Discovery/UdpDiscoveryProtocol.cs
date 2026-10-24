using System.Text;
using System.Text.Json;

namespace HospitalUsers.Services.Discovery;

public static class UdpDiscoveryProtocol
{
    public const string Prefix = "HOSPITALUSERS_DISCOVER_V1 ";
    public const string ServiceName = "HospitalUsers";
    public const string ProtocolName = "hu-discovery-v1";

    // A 16-byte random nonce encoded as exactly 32 uppercase hex digits.
    public static bool TryReadNonce(ReadOnlySpan<byte> datagram, out string nonce)
    {
        nonce = string.Empty;
        if (datagram.Length != Prefix.Length + 32) return false;

        for (int i = 0; i < Prefix.Length; i++)
        {
            if (datagram[i] != (byte)Prefix[i]) return false;
        }

        for (int i = Prefix.Length; i < datagram.Length; i++)
        {
            byte c = datagram[i];
            if (!((c >= (byte)'0' && c <= (byte)'9') ||
                  (c >= (byte)'A' && c <= (byte)'F')))
                return false;
        }

        nonce = Encoding.ASCII.GetString(datagram[Prefix.Length..]);
        return true;
    }

    public static byte[] CreateResponse(string nonce, string apiBaseUrl)
    {
        return JsonSerializer.SerializeToUtf8Bytes(new
        {
            service = ServiceName,
            protocol = ProtocolName,
            nonce,
            apiBaseUrl
        });
    }
}
