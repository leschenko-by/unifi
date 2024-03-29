using System.Net;
using System.Net.NetworkInformation;

namespace Unifi.Gateway.Common.Interfaces
{
    public interface INetworkInfoService
    {
        byte[] LanMacAddress { get; }
        IPAddress LanIPAddress { get; }
        IPAddress LanNetmask { get; }

        byte[] WanMacAddress { get; }
        IPAddress WanIPAddress { get; }
        IPAddress WanNetmask { get; }

        (NetworkInterface, IPInterfaceStatistics) GetLanStatistics();
        (NetworkInterface, IPInterfaceStatistics) GetWanStatistics();
    }
}
