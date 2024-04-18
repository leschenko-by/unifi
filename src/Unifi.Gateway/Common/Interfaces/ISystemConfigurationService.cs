
using Unifi.Gateway.Common.Models;

namespace Unifi.Gateway.Common.Interfaces
{
    public interface ISystemConfigurationService
    {
        Task ApplyAsync(string systemCfg, Configuration configuration);
    }
}
