using Unifi.Gateway.Models;

namespace Unifi.Gateway.Common.Interfaces
{
    public interface IConfigurationWriter
    {
        void SaveConfiguration(Configuration configuration);
    }
}
