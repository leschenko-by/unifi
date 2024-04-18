using System.Net.Sockets;
using Unifi.Gateway.Common.Interfaces;

namespace Unifi.Gateway.BackgroundServices
{
    public class ConnectRequestService(IConnectRequest connectRequest, ILogger<ConnectRequestService> logger) : BackgroundService
    {
        private readonly IConnectRequest connectRequest = connectRequest;
        private readonly ILogger<ConnectRequestService> logger = logger;

        protected override async Task ExecuteAsync(CancellationToken token)
        {
            await Task.Yield();
            while (!token.IsCancellationRequested)
            {
                try
                {
                    using var udp = new UdpClient(connectRequest.Port);

                    while (!token.IsCancellationRequested)
                    {
                        var data = await udp.ReceiveAsync(token);

                        logger.LogInformation("Received connect request from {EndPoint}", data.RemoteEndPoint);

                        connectRequest.Activate();

                        await Task.Delay(1000, token);
                    }
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error receiving connect request");
                }
            }
        }
    }
}
