using Microsoft.Extensions.Options;
using System.Text.Json.Nodes;
using Unifi.Gateway.Common.Interfaces;
using Unifi.Gateway.Options;

namespace Unifi.Gateway.Common.Devices
{
    public class UnifiGatewayDevice : UnifiBaseDevice
    {
        public UnifiGatewayDevice(
            INetworkInfoService network,
            IConfigurationReader configurationReader,
            IConfigurationWriter configurationWriter,
            IOptions<GeneralServiceOptions> serviceOptions) : base(network, configurationReader, configurationWriter, serviceOptions)
        {
        }

        protected override void AddExtraInformMessage(JsonObject message)
        {
            message["config_network_wan"] = new JsonObject
            {
                ["type"] = "dhcp"
            };
            message["config_port_table"] = new JsonArray(
                new JsonObject
                {
                    ["ifname"] = "eth0",
                    ["name"] = "wan",
                },
                new JsonObject
                {
                    ["ifname"] = "eth1",
                    ["name"] = "lan",
                }
            );
        }
    }
}
