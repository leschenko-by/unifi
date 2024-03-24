
using System.Net;

namespace Unifi.Gateway.Common.Interfaces
{
    public interface IUnifiDevice
    {
        byte[] MacAddress { get; }
        string InformUrl { get; }
        byte[] Key { get; }
        IPAddress IPAddress { get; }
        IPAddress Netmask { get; }

        string GetInformMessage();
        void LoadConfigration();
        Task UpdateAsync(string json);
    }
}
