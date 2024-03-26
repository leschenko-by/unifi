
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
        TimeSpan Interval { get; set; }

        Task<string> GetInformMessageAsync();
        void LoadConfigration();
        Task ParseResponseAsync(string json);
        void SaveConfigration();
    }
}
