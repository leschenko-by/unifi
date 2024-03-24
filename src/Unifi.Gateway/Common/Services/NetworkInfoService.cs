using Microsoft.Extensions.Options;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Unifi.Gateway.Common.Interfaces;
using Unifi.Gateway.Options;

namespace Unifi.Gateway.Common.Services
{
    public class NetworkInfoService : INetworkInfoService
    {
        public byte[] MacAddress { get; }
        public IPAddress IPAddress { get; }
        public IPAddress Netmask { get; }

        public NetworkInfoService(IOptions<GeneralServiceOptions> serviceOptions)
        {
            var networks = NetworkInterface.GetAllNetworkInterfaces();
            var network = networks.First(network => network.Id == serviceOptions.Value.NetworkId);

            MacAddress = network.GetPhysicalAddress().GetAddressBytes();

            var address = (from u in network.GetIPProperties().UnicastAddresses
                           where u.Address.AddressFamily == AddressFamily.InterNetwork
                           select u).First();

            IPAddress = address.Address;
            Netmask = address.IPv4Mask;
        }
    }
}
