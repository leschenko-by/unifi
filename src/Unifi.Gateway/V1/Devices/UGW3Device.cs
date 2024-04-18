using System.Text.Json;
using System.Text.Json.Nodes;
using Unifi.Gateway.Common.Models;

namespace Unifi.Gateway.V1.Devices
{
    public class UGW3Device : BaseGatewayV1Device
    {
        public UGW3Device(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            DeviceName = "UGW3";
            DeviceDisplayName = "UniFi Security Gateway";
        }

        protected override async Task AddExtraInformMessage(JsonObject message)
        {
            message["has_eth1"] = true;
            message["has_porta"] = true;
            message["has_dpi"] = true;
            message["has_vti"] = true;
            message["has_ssh_disable"] = true;
            message["fw_caps"] = 3;
            message["guest_token"] = "4C1D46707239C6EB5A2366F505A44A91";
            message["has_default_route_distance"] = true;
            message["has_dnsmasq_hostfile_update"] = false;

            message["config_network_wan"] = JsonSerializer.Deserialize(
                configuration.ConfigNetworkWAN,
                SourceGenerationContext.Default.JsonObject);

            message["uplink"] = "eth0";
            message["config_port_table"] = GetConfigPortTable([0]);
            message["if_table"] = await GetInterfacesAsync([0]);
            message["network_table"] = await GetNetworkTableAsync();
            message["routes"] = await GetRoutesAsync();
        }
    }
}
