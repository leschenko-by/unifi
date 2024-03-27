using System.Net.Sockets;
using Unifi.Gateway.Common.Interfaces;

namespace Unifi.Gateway.Services
{
    public class ConnectRequestService : BackgroundService
    {
        private readonly IConnectRequest connectRequest;
        private readonly ILogger<ConnectRequestService> logger;

        public ConnectRequestService(IConnectRequest connectRequest, ILogger<ConnectRequestService> logger)
        {
            this.connectRequest = connectRequest;
            this.logger = logger;
        }

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

                        logger.LogInformation("UDP: Received connect request from {EndPoint} with {data}",
                            data.RemoteEndPoint, Convert.ToHexString(data.Buffer));

                        connectRequest.IsActive = true;

                        await Task.Delay(1000, token);
                    }
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "UDP: Error receiving connect request");
                }
            }
        }
    }
}
