using System.Net;

namespace Unifi.Gateway.Common.Interfaces
{
    public interface INetworkInfoService
    {
        byte[] MacAddress { get; }
        IPAddress IPAddress { get; }
        IPAddress Netmask { get; }
    }
}
