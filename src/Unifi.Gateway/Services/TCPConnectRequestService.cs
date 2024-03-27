using System.Net;
using System.Net.Sockets;
using System.Text;
using Unifi.Gateway.Common.Interfaces;

namespace Unifi.Gateway.Services
{
    public class TCPConnectRequestService : BackgroundService
    {
        private readonly IConnectRequest connectRequest;
        private readonly ILogger<TCPConnectRequestService> logger;

        public TCPConnectRequestService(IConnectRequest connectRequest, ILogger<TCPConnectRequestService> logger)
        {
            this.connectRequest = connectRequest;
            this.logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken token)
        {
            await Task.Yield();

            using var server = new TcpListener(new IPEndPoint(IPAddress.Any, connectRequest.Port));
            server.Start();

            var buffer = new byte[4 * 1024 * 1024];
            while (!token.IsCancellationRequested)
            {
                try
                {
                    using var client = await server.AcceptTcpClientAsync(token);
                    var stream = client.GetStream();
                    int read = stream.Read(buffer, 0, buffer.Length);

                    logger.LogInformation("TCP: Received connect request from {EndPoint} with\n{data}",
                        client.Client.RemoteEndPoint, Encoding.UTF8.GetString(buffer[..read]));

                    stream.Write(Encoding.UTF8.GetBytes("HTTP/1.1 200\r\n\r\n"));
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "TCP: Error receiving connect request");
                }

                connectRequest.IsActive = true;

                await Task.Delay(1000, token);
            }
        }
    }
}
