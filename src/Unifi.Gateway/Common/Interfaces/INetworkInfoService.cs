using System.Net;

namespace Unifi.Gateway.Common.Interfaces
{
    public interface INetworkInfoService
    {
        public IReadOnlyList<IEthernetInterface?> Interfaces { get; }

        string GetAddress(IPAddress address, IPAddress netmask);
        string GetNetwork(IPAddress address, IPAddress netmask);
        int GetNetworkLength(IPAddress netmask);
        void RefreshInterfaces();
    }
}
