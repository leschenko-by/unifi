using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Unifi.Gateway.Common.Interfaces;
using Unifi.Gateway.Options;

namespace Unifi.Gateway.Services
{

    public class DiscoveryService(INetworkInfoService network, IOptions<GeneralServiceOptions> options) : BackgroundService
    {
        private readonly DateTime startTime = DateTime.Now;
        private readonly INetworkInfoService network = network;
        private readonly IOptions<GeneralServiceOptions> options = options;

        protected override async Task ExecuteAsync(CancellationToken token)
        {
            await Task.Yield();

            var macAddress = network.MacAddress;
            var ipAddress = network.IPAddress;
            if (ipAddress is null) return;

            var endPoint = new IPEndPoint(IPAddress.Parse("233.89.188.1"), 10001);
            var client = new UdpClient(new IPEndPoint(ipAddress, 0));

            var periodic = new PeriodicTimer(TimeSpan.FromSeconds(1));

            int broadcastIndex = 0;
            while (!token.IsCancellationRequested)
            {
                var datagram = BuildDatagram(broadcastIndex, macAddress, ipAddress.GetAddressBytes());
                await client.SendAsync(datagram, datagram.Length, endPoint);

                await periodic.WaitForNextTickAsync(token);

                broadcastIndex = (broadcastIndex + 1) % 20;
            }
        }

        private byte[] BuildDatagram(int broadcastIndex, byte[] macAddress, byte[] ipAddress)
        {
            var uptime = (int)(DateTime.Now - startTime).TotalSeconds;

            var device = options.Value.Device;
            var firmware = options.Value.Firmware;

            var builder = new UnifyDatagramBuilder();
            builder.Add(1, macAddress);
            builder.Add(2, [.. macAddress, .. ipAddress]);
            builder.Add(3, Encoding.ASCII.GetBytes($"{device}.v{firmware}"));
            builder.Add(10, BitConverter.GetBytes(uptime).Reverse().ToArray());
            builder.Add(11, Encoding.ASCII.GetBytes("UBNT"));
            builder.Add(12, Encoding.ASCII.GetBytes(device));
            builder.Add(19, macAddress);
            builder.Add(18, BitConverter.GetBytes(broadcastIndex).Reverse().ToArray());
            builder.Add(21, Encoding.ASCII.GetBytes(device));
            builder.Add(27, Encoding.ASCII.GetBytes(firmware));
            builder.Add(22, Encoding.ASCII.GetBytes(firmware));

            return builder.Build();
        }
    }
}