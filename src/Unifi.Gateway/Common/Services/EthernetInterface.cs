using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Unifi.Gateway.Common.Interfaces;

namespace Unifi.Gateway.Common.Services
{
    public class EthernetInterface : IEthernetInterface
    {
        private readonly NetworkInterface eth;

        public byte[] MacAddress { get; }
        public IPAddress IPAddress { get; }
        public IPAddress Netmask { get; }

        public EthernetInterface(NetworkInterface eth)
        {
            this.eth = eth;

            var address = eth.GetIPProperties()
                .UnicastAddresses
                .FirstOrDefault(u => u.Address.AddressFamily == AddressFamily.InterNetwork);

            if (address != null)
            {
                IPAddress = address.Address;
                Netmask = address.IPv4Mask;
                MacAddress = eth.GetPhysicalAddress().GetAddressBytes();
            }
            else
            {
                IPAddress = IPAddress.None;
                Netmask = IPAddress.None;
                MacAddress = eth.GetPhysicalAddress().GetAddressBytes();
            }
        }

        public IPInterfaceStatistics GetIPStatistics()
        {
            return eth.GetIPStatistics();
        }
    }
}
