using Unifi.Gateway.Common.Interfaces;
using Unifi.Gateway.Models;

namespace Unifi.Gateway.Devices
{
    public abstract class BaseGatewayV2Device(IServiceProvider serviceProvider) : BaseUnifiDevice(serviceProvider)
    {
        protected override async Task ApplySystemConfigurationAsync(InformResponseMessage data)
        {
            var configurator = serviceProvider.GetRequiredKeyedService<ISystemConfigurationService>("v2");
            await configurator.ApplyAsync(data.SystemCfg, configuration);
        }
    }
}
