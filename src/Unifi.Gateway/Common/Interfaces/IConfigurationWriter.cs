using Unifi.Gateway.Json;

namespace Unifi.Gateway.Common.Interfaces
{
    public interface IConfigurationWriter
    {
        void SaveConfiguration(Configuration configuration);
    }
}
