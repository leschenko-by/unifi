using Unifi.Gateway.Common.Models;

namespace Unifi.Gateway.Common.Interfaces
{
    public interface IConfigurationReader
    {
        Configuration LoadConfiguration();
    }
}
