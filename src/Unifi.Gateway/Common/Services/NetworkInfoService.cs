using Microsoft.Extensions.Options;
using System.Net;
using System.Net.NetworkInformation;
using Unifi.Gateway.Common.Interfaces;
using Unifi.Gateway.Common.Models;

namespace Unifi.Gateway.Common.Services
{
    public class NetworkInfoService : INetworkInfoService
    {
        private readonly string[] ports;

        public IReadOnlyList<IEthernetInterface?> PortInterfaces { get; private set; } = [];
        public IReadOnlyList<IEthernetInterface> AllInterfaces { get; private set; } = [];

        public NetworkInfoService(IOptions<GeneralServiceOptions> serviceOptions)
        {
            ports = serviceOptions.Value.Ports.Split(",");
            RefreshInterfaces();
        }

        public void RefreshInterfaces()
        {
            var networks = NetworkInterface.GetAllNetworkInterfaces();

            var interfaces = new List<IEthernetInterface?>();
            var index = 0;
            foreach (var port in ports)
            {
                var localNic = port;
                string[] extraNics = [];
                if (localNic.Contains('+'))
                {
                    var data = localNic.Split('+');
                    localNic = data[0];
                    extraNics = [.. data.Skip(1)];
                }

                var eth = networks.FirstOrDefault(n => n.Id == localNic);
                if (eth != null)
                {
                    var extraEths = extraNics
                        .Select(nicId => networks.FirstOrDefault(n => n.Id == nicId))
                        .Where(nic => nic != null)
                        .OfType<NetworkInterface>()
                        .ToArray();

                    var mac = eth.GetPhysicalAddress().GetAddressBytes();
                    interfaces.Add(new EthernetInterface(eth, "eth" + index, mac, extraEths));
                }
                else
                {
                    interfaces.Add(null);
                }

                index++;
            }

            PortInterfaces = interfaces.AsReadOnly();
            AllInterfaces = networks
                .Select(eth =>
                {
                    var mac = eth.GetPhysicalAddress().GetAddressBytes();
                    return new EthernetInterface(eth, eth.Id, mac, []);
                })
                .ToList()
                .AsReadOnly();
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
