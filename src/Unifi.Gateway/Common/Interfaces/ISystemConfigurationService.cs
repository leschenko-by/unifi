
namespace Unifi.Gateway.Common.Interfaces
{
    public interface ISystemConfigurationService
    {
        Task ApplyAsync(string systemCfg);
    }
}
