using Unifi.Gateway.Common.Models;

namespace Unifi.Gateway.Common.Interfaces
{
    public interface IConfigurationWriter
    {
        void SaveConfiguration(Configuration configuration);
    }
}
