using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Unifi.Gateway.Common.Interfaces;

namespace Unifi.Gateway.Common.Services
{
    public class EthernetInterface(
        NetworkInterface eth, 
        string unifiNic, 
        byte[] mac, 
        NetworkInterface[] eths) : IEthernetInterface
    {
        private readonly NetworkInterface eth = eth;
        private readonly NetworkInterface[] eths = eths;

        public string UnifiNic { get; } = unifiNic;

        public string LocalNic => eth.Id;

        public byte[] MacAddress => mac;

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

        public EthernetInterfaceStats GetIPStatistics()
        {
            var stats = (EthernetInterfaceStats)eth.GetIPStatistics();
            foreach (var otherEth in eths.Where(e => e.Id != eth.Id))
            {
                stats += (EthernetInterfaceStats)otherEth.GetIPStatistics();
            }
            return stats;
        }
    }

    public record EthernetInterfaceStats(
        long BytesReceived,
        long BytesSent,
        long IncomingPacketsDiscarded,
        long IncomingPacketsWithErrors,
        long NonUnicastPacketsReceived,
        long UnicastPacketsReceived,
        long OutgoingPacketsWithErrors,
        long UnicastPacketsSent)
    {
        public static implicit operator EthernetInterfaceStats(IPInterfaceStatistics stats) => new(
            stats.BytesReceived,
            stats.BytesSent,
            stats.IncomingPacketsDiscarded,
            stats.IncomingPacketsWithErrors,
            stats.NonUnicastPacketsReceived,
            stats.UnicastPacketsReceived,
            stats.OutgoingPacketsWithErrors,
            stats.UnicastPacketsSent);

        public static EthernetInterfaceStats operator +(EthernetInterfaceStats left, EthernetInterfaceStats right) => new(
            left.BytesReceived + right.BytesReceived,
            left.BytesSent + right.BytesSent,
            left.IncomingPacketsDiscarded + right.IncomingPacketsDiscarded,
            left.IncomingPacketsWithErrors + right.IncomingPacketsWithErrors,
            left.NonUnicastPacketsReceived + right.NonUnicastPacketsReceived,
            left.UnicastPacketsReceived + right.UnicastPacketsReceived,
            left.OutgoingPacketsWithErrors + right.OutgoingPacketsWithErrors,
            left.UnicastPacketsSent + right.UnicastPacketsSent);
    }
}
