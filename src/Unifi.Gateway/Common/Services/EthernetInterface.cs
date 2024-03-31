using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Unifi.Gateway.Common.Interfaces;

namespace Unifi.Gateway.Common.Services
{
    public class EthernetInterface : IEthernetInterface
    {
        private readonly NetworkInterface eth;

        public string Id => eth.Id;
        public byte[] MacAddress { get; }
        public IPAddress IPAddress { get; } = IPAddress.None;
        public IPAddress Netmask { get; } = IPAddress.None;
        public IPAddress[] Gateways { get; } = [];
        public IPAddress[] DnsAddresses { get; } = [];

        public EthernetInterface(NetworkInterface eth)
        {
            this.eth = eth;

            MacAddress = eth.GetPhysicalAddress().GetAddressBytes();

            var address = eth.GetIPProperties().UnicastAddresses
                .FirstOrDefault(u => u.Address.AddressFamily == AddressFamily.InterNetwork);

            if (address != null)
            {
                IPAddress = address.Address;
                Netmask = address.IPv4Mask;
                Gateways = eth.GetIPProperties().GatewayAddresses
                        .Select(gateway => gateway.Address)
                        .Where(t => t.AddressFamily == AddressFamily.InterNetwork)
                        .ToArray();
                DnsAddresses = eth.GetIPProperties().DnsAddresses.Where(t => t.AddressFamily == AddressFamily.InterNetwork).ToArray();
            }
        }

        public IPInterfaceStatistics GetIPStatistics()
        {
            return eth.GetIPStatistics();
        }
    }
}
