using Unifi.Gateway.Models;

namespace Unifi.Gateway.Common.Interfaces
{
    public interface IConfigurationReader
    {
        Configuration LoadConfiguration();
    }
}
