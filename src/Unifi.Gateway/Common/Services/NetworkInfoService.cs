using Microsoft.Extensions.Options;
using System.Net;
using System.Net.NetworkInformation;
using Unifi.Gateway.Common.Interfaces;
using Unifi.Gateway.Models;

namespace Unifi.Gateway.Common.Services
{
    public class NetworkInfoService : INetworkInfoService
    {
        private readonly string[] ports;

        public IReadOnlyList<IEthernetInterface?> Interfaces { get; private set; } = [];

        public NetworkInfoService(IOptions<GeneralServiceOptions> serviceOptions)
        {
            ports = serviceOptions.Value.Ports.Split(",");
            RefreshInterfaces();
        }

        public void RefreshInterfaces()
        {
            var networks = NetworkInterface.GetAllNetworkInterfaces();
            Interfaces = ports.Select(id => networks.FirstOrDefault(n => n.Id == id))
                .Select((t, index) => t != null ? new EthernetInterface(t, "eth" + index) : null)
                .ToList().AsReadOnly();
        }

        public string GetNetwork(IPAddress address, IPAddress netmask)
        {
            var addr = address.GetAddressBytes();
            var mask = netmask.GetAddressBytes();

            var result = new byte[addr.Length];
            for (var i = 0; i < addr.Length; i++)
            {
                result[i] = (byte)(addr[i] & mask[i]);
            }

            int length = GetNetworkLength(netmask);
            return string.Join(".", result.Select(t => t.ToString())) + "/" + length;
        }

        public string GetAddress(IPAddress address, IPAddress netmask)
        {
            int length = GetNetworkLength(netmask);
            return address + "/" + length;
        }

        public int GetNetworkLength(IPAddress netmask)
        {
            var length = 0;
            foreach (var o in netmask.GetAddressBytes())
            {
                var bits = o;
                while ((bits & 0x80) != 0)
                {
                    length++;
                    bits = (byte)((bits << 1) & 0xff);
                }
            }

            return length;
        }
    }
}
