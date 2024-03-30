using Microsoft.Extensions.Options;
using System.Net.NetworkInformation;
using Unifi.Gateway.Common.Interfaces;
using Unifi.Gateway.Models;

namespace Unifi.Gateway.Common.Services
{
   public class NetworkInfoService : INetworkInfoService
    {
        public IReadOnlyList<IEthernetInterface?> Interfaces { get; }

        public NetworkInfoService(IOptions<GeneralServiceOptions> serviceOptions)
        {
            var ids = serviceOptions.Value.Ports.Split(",");
            var networks = NetworkInterface.GetAllNetworkInterfaces();
            Interfaces = ids.Select(id => networks.FirstOrDefault(n => n.Id == id))
                .Select(t => t != null ? new EthernetInterface(t) : null)
                .ToList().AsReadOnly();
        }
    }
}
