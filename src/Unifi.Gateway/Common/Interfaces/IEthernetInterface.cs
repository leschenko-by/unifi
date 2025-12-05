using System.Net;
using Unifi.Gateway.Common.Services;

namespace Unifi.Gateway.Common.Interfaces
{
    public interface IEthernetInterface
    {
        string LocalNic { get; }
        byte[] MacAddress { get; }
        IPAddress IPAddress { get; }
        IPAddress Netmask { get; }
        IPAddress[] Gateways { get; }
        IPAddress[] DnsAddresses { get; }
        string UnifiNic { get; }

        EthernetInterfaceStats GetIPStatistics();
    }
}
