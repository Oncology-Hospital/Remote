using System.Net;
using System.Net.Sockets;
using System.Text;

namespace RemoteDesktop.Server;

public sealed class LanDiscoveryBroadcaster : BackgroundService
{
    public const int DiscoveryPort = 50505;
    public const int ServerPort = 5000;
    public const string DiscoveryMessage = "REMOTE_DESKTOP_SERVER";

    private readonly ILogger<LanDiscoveryBroadcaster> _logger;

    public LanDiscoveryBroadcaster(ILogger<LanDiscoveryBroadcaster> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var client = new UdpClient
        {
            EnableBroadcast = true
        };

        var endpoint = new IPEndPoint(IPAddress.Broadcast, DiscoveryPort);
        var payload = Encoding.UTF8.GetBytes($"{DiscoveryMessage}|{ServerPort}");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await client.SendAsync(payload, endpoint, stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogDebug(ex, "LAN discovery broadcast failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }
}
