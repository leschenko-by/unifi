using System.Net;
using System.Net.NetworkInformation;

namespace Unifi.Gateway.Common.Interfaces
{
    public interface IEthernetInterface
    {
        byte[] MacAddress { get; }
        IPAddress IPAddress { get; }
        IPAddress Netmask { get; }

        IPInterfaceStatistics GetIPStatistics();
    }
}
