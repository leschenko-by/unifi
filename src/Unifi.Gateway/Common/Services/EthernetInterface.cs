using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Unifi.Gateway.Common.Interfaces;

namespace Unifi.Gateway.Common.Services
{
    public class EthernetInterface(NetworkInterface eth, string unifiNic) : IEthernetInterface
    {
        private readonly NetworkInterface eth = eth;

        public string UnifiNic { get; } = unifiNic;

        public string LocalNic => eth.Id;

        public byte[] MacAddress => eth.GetPhysicalAddress().GetAddressBytes();

        public IPAddress IPAddress => eth.GetIPProperties().UnicastAddresses
            .FirstOrDefault(u => u.Address.AddressFamily == AddressFamily.InterNetwork)?
            .Address ?? IPAddress.None;

        public IPAddress Netmask => eth.GetIPProperties().UnicastAddresses
            .FirstOrDefault(u => u.Address.AddressFamily == AddressFamily.InterNetwork)?
            .IPv4Mask ?? IPAddress.None;

        public IPAddress[] Gateways => eth.GetIPProperties().GatewayAddresses
            .Select(gateway => gateway.Address)
            .Where(t => t.AddressFamily == AddressFamily.InterNetwork)
            .ToArray();

        public IPAddress[] DnsAddresses => eth.GetIPProperties().DnsAddresses
            .Where(t => t.AddressFamily == AddressFamily.InterNetwork)
            .ToArray();

        public IPInterfaceStatistics GetIPStatistics()
        {
            return eth.GetIPStatistics();
        }
    }
}
