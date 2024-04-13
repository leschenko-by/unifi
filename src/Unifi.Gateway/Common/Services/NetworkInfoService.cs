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
        private readonly ILogger<NetworkInfoService> logger;

        public IReadOnlyList<IEthernetInterface?> Interfaces { get; private set; } = [];

        public NetworkInfoService(
            IOptions<GeneralServiceOptions> serviceOptions,
            ILogger<NetworkInfoService> logger)
        {
            ports = serviceOptions.Value.Ports.Split(",");
            this.logger = logger;
            RefreshInterfaces();
        }

        public void RefreshInterfaces()
        {
            var networks = NetworkInterface.GetAllNetworkInterfaces();
            foreach (var nic in networks)
            {
                logger.LogInformation("network interface: {id} {name} {type} {status}",
                    nic.Id, nic.Name, nic.NetworkInterfaceType, nic.OperationalStatus);
            }

            var interfaces = new List<IEthernetInterface?>();
            var index = 0;
            foreach (var port in ports)
            {
                var localNic = port;
                var pppoeNic = string.Empty;
                if (localNic.Contains('/'))
                {
                    var data = localNic.Split('/');
                    localNic = data[0];
                    pppoeNic = data[1];
                }

                var eth = networks.FirstOrDefault(n => n.Id == localNic);
                var pppoe = string.IsNullOrEmpty(pppoeNic)
                    ? null
                    : networks.FirstOrDefault(n => n.Id == pppoeNic);

                if (eth != null)
                {
                    var nic = pppoe ?? eth;
                    var mac = eth.GetPhysicalAddress().GetAddressBytes();
                    interfaces.Add(new EthernetInterface(nic, "eth" + index, mac));
                }
                else
                {
                    interfaces.Add(null);
                }

                index++;
            }

            Interfaces = interfaces.AsReadOnly();
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
