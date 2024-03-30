using System.Text.Json;
using System.Text.Json.Nodes;
using Unifi.Gateway.Common.Interfaces;
using Unifi.Gateway.Json;
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

        protected override async Task AddExtraInformMessage(JsonObject message)
        {
            await Task.Yield();

            message["vpn"] = new JsonArray();

            message["config_network_wan"] = JsonSerializer.Deserialize(
                configuration.ConfigNetworkWAN,
                SourceGenerationContext.Default.JsonObject);

            message["config_network_wan2"] = JsonSerializer.Deserialize(
                configuration.ConfigNetworkWAN2,
                SourceGenerationContext.Default.JsonObject);
        }
    }
}
