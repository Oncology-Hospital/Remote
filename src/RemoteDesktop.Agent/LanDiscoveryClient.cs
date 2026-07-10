using System.Net;
using System.Net.Sockets;
using System.Text;

namespace RemoteDesktop.Agent;

internal static class LanDiscoveryClient
{
    public const int DiscoveryPort = 50505;
    private const int ServerPort = 5000;
    private const string DiscoveryMessage = "REMOTE_DESKTOP_SERVER";

    public static async Task<string?> WaitForServerUrlAsync(CancellationToken cancellationToken)
    {
        using var client = new UdpClient(AddressFamily.InterNetwork);
        client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        client.Client.Bind(new IPEndPoint(IPAddress.Any, DiscoveryPort));

        while (!cancellationToken.IsCancellationRequested)
        {
            var result = await client.ReceiveAsync(cancellationToken);
            var message = Encoding.UTF8.GetString(result.Buffer);
            var parts = message.Split('|', StringSplitOptions.TrimEntries);

            if (parts.Length < 2 || parts[0] != DiscoveryMessage || !int.TryParse(parts[1], out var port))
            {
                continue;
            }

            if (port != ServerPort)
            {
                continue;
            }

            return $"http://{result.RemoteEndPoint.Address}:{port}/remoteHub";
        }

        return null;
    }
}
