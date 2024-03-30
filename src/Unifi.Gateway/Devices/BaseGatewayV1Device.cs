using Unifi.Gateway.Common.Interfaces;
using Unifi.Gateway.Models;

namespace Unifi.Gateway.Devices
{
    public abstract class BaseGatewayV1Device(IServiceProvider serviceProvider) : BaseUnifiDevice(serviceProvider)
    {
        protected override async Task ApplySystemConfigurationAsync(InformResponseMessage data)
        {
            var configurator = serviceProvider.GetRequiredKeyedService<ISystemConfigurationService>("v1");
            await configurator.ApplyAsync(data.SystemCfg, configuration);
        }
    }
}
