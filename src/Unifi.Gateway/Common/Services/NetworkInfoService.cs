using Microsoft.Extensions.Options;
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
    }
}
