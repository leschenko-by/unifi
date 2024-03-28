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
        private readonly IOptions<GeneralServiceOptions> serviceOptions;

        public byte[] LanMacAddress { get; }
        public IPAddress LanIPAddress { get; }
        public IPAddress LanNetmask { get; }

        public byte[] WanMacAddress { get; }
        public IPAddress WanIPAddress { get; }
        public IPAddress WanNetmask { get; }

        public NetworkInfoService(IOptions<GeneralServiceOptions> serviceOptions)
        {
            this.serviceOptions = serviceOptions;

            var networks = NetworkInterface.GetAllNetworkInterfaces();
            var lan = networks.FirstOrDefault(network => network.Id == serviceOptions.Value.LanNetworkId);
            var wan = networks.First(network => network.Id == serviceOptions.Value.WanNetworkId);

            if (lan != null)
            {
                LanMacAddress = lan.GetPhysicalAddress().GetAddressBytes();
                var address = (from u in lan.GetIPProperties().UnicastAddresses
                               where u.Address.AddressFamily == AddressFamily.InterNetwork
                               select u).First();

                LanIPAddress = address.Address;
                LanNetmask = address.IPv4Mask;
            }
            else
            {
                LanMacAddress = new byte[6];
                LanIPAddress = IPAddress.Any;
                LanNetmask = IPAddress.None;
            }

            if (wan != null)
            {
                var address = (from u in wan.GetIPProperties().UnicastAddresses
                               where u.Address.AddressFamily == AddressFamily.InterNetwork
                               select u).First();

                WanIPAddress = address.Address;
                WanNetmask = address.IPv4Mask;
                WanMacAddress = wan.GetPhysicalAddress().GetAddressBytes();
            }
            else
            {
                WanIPAddress = IPAddress.Any;
                WanNetmask = IPAddress.None;
                WanMacAddress = new byte[6];
            }
        }

        public (NetworkInterface?, IPInterfaceStatistics?) GetLanStatistics()
        {
            var networks = NetworkInterface.GetAllNetworkInterfaces();
            var lan = networks.FirstOrDefault(network => network.Id == serviceOptions.Value.LanNetworkId);
            return (lan, lan?.GetIPStatistics());
        }

        public (NetworkInterface?, IPInterfaceStatistics?) GetWanStatistics()
        {
            var networks = NetworkInterface.GetAllNetworkInterfaces();
            var wan = networks.FirstOrDefault(network => network.Id == serviceOptions.Value.WanNetworkId);
            return (wan, wan?.GetIPStatistics());
        }
    }
}
