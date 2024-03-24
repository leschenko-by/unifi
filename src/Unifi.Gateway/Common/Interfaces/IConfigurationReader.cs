using Unifi.Gateway.Json;

namespace Unifi.Gateway.Common.Interfaces
{
    public interface IConfigurationReader
    {
        AdoptOptions LoadConfiguration();
    }
}
