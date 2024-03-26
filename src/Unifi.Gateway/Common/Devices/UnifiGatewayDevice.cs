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
                },
                new JsonObject
                {
                    ["ifname"] = "eth2",
                    ["name"] = "wan2",
                }
            );

            var lan = network.GetLanStatistics();
            var wan = network.GetWanStatistics();

#pragma warning disable CA1416 // Validate platform compatibility
            message["if_table"] = new JsonArray(
                new JsonObject
                {
                    ["full_duplex"] = true,
                    ["name"] = "eth0",
                    ["ip"] = network.WanIPAddress.ToString(),
                    ["mac"] = string.Join(":", network.WanMacAddress.Select(t => t.ToString("x2"))),
                    ["netmask"] = "0.0.0.0",
                    ["up"] = true,
                    ["num_port"] = 0,
                    ["rx_bytes"] = wan?.BytesReceived ?? 0,
                    ["rx_dropped"] = wan?.IncomingPacketsDiscarded ?? 0,
                    ["rx_errors"] = wan?.IncomingPacketsWithErrors ?? 0,
                    ["rx_multicast"] = wan?.NonUnicastPacketsReceived ?? 0,
                    ["rx_packets"] = wan?.UnicastPacketsReceived ?? 0,
                    ["speed"] = 1000,
                    ["tx_bytes"] = wan?.BytesSent ?? 0,
                    ["tx_dropped"] = wan?.OutgoingPacketsDiscarded ?? 0,
                    ["tx_errors"] = wan?.OutgoingPacketsWithErrors ?? 0,
                    ["tx_packets"] = wan?.UnicastPacketsSent ?? 0,
                },
                new JsonObject
                {
                    ["full_duplex"] = true,
                    ["name"] = "eth1",
                    ["ip"] = IPAddress.ToString(),
                    ["mac"] = MacAddressString,
                    ["netmask"] = Netmask.ToString(),
                    ["up"] = true,
                    ["num_port"] = 0,
                    ["rx_bytes"] = lan?.BytesReceived ?? 0,
                    ["rx_dropped"] = lan?.IncomingPacketsDiscarded ?? 0,
                    ["rx_errors"] = lan?.IncomingPacketsWithErrors ?? 0,
                    ["rx_multicast"] = lan?.NonUnicastPacketsReceived ?? 0,
                    ["rx_packets"] = lan?.UnicastPacketsReceived ?? 0,
                    ["speed"] = 1000,
                    ["tx_bytes"] = lan?.BytesSent ?? 0,
                    ["tx_dropped"] = lan?.OutgoingPacketsDiscarded ?? 0,
                    ["tx_errors"] = lan?.OutgoingPacketsWithErrors ?? 0,
                    ["tx_packets"] = lan?.UnicastPacketsSent ?? 0,
                },
                new JsonObject
                {
                    ["name"] = "eth2",
                    ["enable"] = false,
                }
            );
#pragma warning restore CA1416 // Validate platform compatibility
        }
    }
}
