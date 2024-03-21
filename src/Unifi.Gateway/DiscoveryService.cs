using Microsoft.Extensions.Options;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace Unifi.Gateway
{
    public class DiscoveryServiceOptions
    {
        public string Device { get; set; } = "UGW3";
        public string Firmware { get; set; } = "4.4.44.5213871";
        public string NetworkId { get; set; } = string.Empty;
    }

    public class DiscoveryService : BackgroundService
    {
        private readonly long startTime = DateTime.Now.Ticks;
        private readonly IOptions<DiscoveryServiceOptions> options;

        public DiscoveryService(IOptions<DiscoveryServiceOptions> options)
        {
            this.options = options;
        }

        protected override async Task ExecuteAsync(CancellationToken token)
        {
            await Task.Yield();

            var networks = NetworkInterface.GetAllNetworkInterfaces();
            var network = networks.First(network => network.Id == options.Value.NetworkId);

            var macAddress = network.GetPhysicalAddress().GetAddressBytes();
            var ipAddress = GetIPAddress(network);
            if (ipAddress is null) return;

            var endPoint = new IPEndPoint(IPAddress.Parse("233.89.188.1"), 10001);
            var client = new UdpClient(new IPEndPoint(ipAddress, 0));

            int broadcastIndex = 0;
            while (!token.IsCancellationRequested)
            {
                var datagram = BuildDatagram(broadcastIndex, macAddress, ipAddress.GetAddressBytes());
                await client.SendAsync(datagram, datagram.Length, endPoint);

                await Task.Delay(1000, token);

                broadcastIndex = (broadcastIndex + 1) % 20;
            }
        }

        private byte[] BuildDatagram(int broadcastIndex, byte[] macAddress, byte[] ipAddress)
        {
            var uptime = (int)(DateTime.Now.Ticks - startTime);

            var device = options.Value.Device;
            var firmware = options.Value.Firmware;

            var builder = new UnifyDatagramBuilder();
            builder.Add(1, macAddress);
            builder.Add(2, [..macAddress, ..ipAddress]);
            builder.Add(3, Encoding.ASCII.GetBytes($"{device}.v{firmware}"));
            builder.Add(10, GetBytes(uptime));
            builder.Add(11, Encoding.ASCII.GetBytes("UBNT"));
            builder.Add(12, Encoding.ASCII.GetBytes(device));
            builder.Add(19, macAddress);
            builder.Add(18, GetBytes(broadcastIndex));
            builder.Add(21, Encoding.ASCII.GetBytes(device));
            builder.Add(27, Encoding.ASCII.GetBytes(firmware));
            builder.Add(22, Encoding.ASCII.GetBytes(firmware));

            return builder.Build();

            static byte[] GetBytes(int value)
            {
                var data = new byte[4];
                for (var i = 0; i < 4; i++)
                {
                    data[i] = (byte)(value & 255);

                    value >>= 8;
                }
                return data;
            }
        }

        private static IPAddress? GetIPAddress(NetworkInterface network) =>
            (from address in network.GetIPProperties().UnicastAddresses
             where address.Address.AddressFamily == AddressFamily.InterNetwork
             select address.Address).FirstOrDefault();
    }
}