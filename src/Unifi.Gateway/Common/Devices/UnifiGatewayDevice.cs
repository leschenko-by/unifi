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
            message["has_dpi"] = true;
            message["has_vti"] = true;
            message["has_ssh_disable"] = true;
            message["has_crash_logs"] = false;
            message["fw_caps"] = 3;
            message["has_default_route_distance"] = true;
            message["config_network_wan"] = new JsonObject
            {
                ["type"] = "dhcp"
            };
            message["vpn"] = new JsonArray();
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
            message["if_table"] = new JsonArray(
                new JsonArray
                {
                    ["full_duplex"] = true,
                    ["name"] = "eth0",
                    ["ip"] = "134.17.26.13",
                    ["mac"] = "00:15:5d:02:09:02",
                    ["netmask"] = "0.0.0.0",
                    ["up"] = true,
                    ["num_port"] = 0,
                    ["rx_bytes"] = 137376,
                    ["rx_dropped"] = 0,
                    ["rx_errors"] = 0,
                    ["rx_multicast"] = 0,
                    ["rx_packets"] = 794,
                    ["speed"] = 1000,
                    ["tx_bytes"] = 275576,
                    ["tx_dropped"] = 0,
                    ["tx_errors"] = 0,
                    ["tx_packets"] = 1197,
                },
                new JsonArray
                {
                    ["full_duplex"] = true,
                    ["name"] = "eth1",
                    ["ip"] = IPAddress.ToString(),
                    ["mac"] = MacAddressString,
                    ["netmask"] = Netmask.ToString(),
                    ["up"] = true,
                    ["num_port"] = 0,
                    ["rx_bytes"] = 137376,
                    ["rx_dropped"] = 0,
                    ["rx_errors"] = 0,
                    ["rx_multicast"] = 0,
                    ["rx_packets"] = 794,
                    ["speed"] = 1000,
                    ["tx_bytes"] = 275576,
                    ["tx_dropped"] = 0,
                    ["tx_errors"] = 0,
                    ["tx_packets"] = 1197,
                }
            );
        }
    }
}
