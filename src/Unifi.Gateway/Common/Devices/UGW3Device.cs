using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json.Nodes;

namespace Unifi.Gateway.Common.Devices
{
    public class UGW3Device : BaseUnifiDevice
    {
        public UGW3Device(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            DeviceName = "UGW3";
            DeviceDisplayName = "UniFi Security Gateway";
        }

        protected override void AddExtraInformMessage(JsonObject message)
        {
            message["has_dpi"] = false;
            message["has_vti"] = false;
            message["has_ssh_disable"] = true;
            message["fw_caps"] = 3;
            message["guest_token"] = "4C1D46707239C6EB5A2366F505A44A91";
            message["has_default_route_distance"] = true;
            message["has_dnsmasq_hostfile_update"] = false;
            message["config_network_wan"] = new JsonObject
            {
                ["type"] = "dhcp"
            };
            message["uplink"] = "eth2";
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

            var (_, lanStats) = network.GetLanStatistics();
            var (wan, wanStats) = network.GetWanStatistics();
            var gateways = wan?.GetIPProperties().GatewayAddresses
                .Select(gateway => gateway.Address)
                .Where(t=>t.AddressFamily == AddressFamily.InterNetwork)
                .Select(address => address.ToString())
                .ToArray();

#pragma warning disable CA1416 // Validate platform compatibility
            message["if_table"] = new JsonArray(
                new JsonObject
                {
                    ["full_duplex"] = true,
                    ["name"] = "eth0",
                    ["enable"] = true,
                    ["ip"] = network.WanIPAddress.ToString(),
                    ["gateways"] = new JsonArray(gateways?.Select(t => JsonValue.Create(t)).ToArray() ?? []),
                    ["mac"] = string.Join(":", network.WanMacAddress.Select(t => t.ToString("x2"))),
                    ["netmask"] = network.WanNetmask.ToString(),
                    ["up"] = wan?.OperationalStatus == OperationalStatus.Up,
                    ["num_port"] = 3,
                    ["rx_bytes"] = wanStats?.BytesReceived ?? 0,
                    ["rx_dropped"] = wanStats?.IncomingPacketsDiscarded ?? 0,
                    ["rx_errors"] = wanStats?.IncomingPacketsWithErrors ?? 0,
                    ["rx_multicast"] = wanStats?.NonUnicastPacketsReceived ?? 0,
                    ["rx_packets"] = wanStats?.UnicastPacketsReceived ?? 0,
                    ["speed"] = 1000,
                    ["tx_bytes"] = wanStats?.BytesSent ?? 0,
                    ["tx_dropped"] = wanStats?.OutgoingPacketsDiscarded ?? 0,
                    ["tx_errors"] = wanStats?.OutgoingPacketsWithErrors ?? 0,
                    ["tx_packets"] = wanStats?.UnicastPacketsSent ?? 0,

                    ["latency"] = 1,
                    ["uptime"] = Environment.TickCount64 / 1000,

                    ["speedtest_lastrun"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 60,
                    ["speedtest_ping"] = 18,
                    ["speedtest_status"] = "Idle",
                    ["xput_down"] = 321,
                    ["xput_up"] = 123,
                },
                new JsonObject
                {
                    ["full_duplex"] = true,
                    ["name"] = "eth1",
                    ["enable"] = true,
                    ["ip"] = IPAddress.ToString(),
                    ["mac"] = MacAddressString,
                    ["netmask"] = Netmask.ToString(),
                    ["up"] = true,
                    ["num_port"] = 1,
                    ["rx_bytes"] = lanStats?.BytesReceived ?? 0,
                    ["rx_dropped"] = lanStats?.IncomingPacketsDiscarded ?? 0,
                    ["rx_errors"] = lanStats?.IncomingPacketsWithErrors ?? 0,
                    ["rx_multicast"] = lanStats?.NonUnicastPacketsReceived ?? 0,
                    ["rx_packets"] = lanStats?.UnicastPacketsReceived ?? 0,
                    ["speed"] = 1000,
                    ["tx_bytes"] = lanStats?.BytesSent ?? 0,
                    ["tx_dropped"] = lanStats?.OutgoingPacketsDiscarded ?? 0,
                    ["tx_errors"] = lanStats?.OutgoingPacketsWithErrors ?? 0,
                    ["tx_packets"] = lanStats?.UnicastPacketsSent ?? 0,
                },
                new JsonObject
                {
                    ["name"] = "eth2",
                    ["enable"] = false,
                    ["num_port"] = 2,
                }
            );
            message["network_table"] = new JsonArray(
                new JsonObject
                {
                    ["autoneg"] = true,
                    ["duplex"] = "full",
                    ["name"] = "eth0",
                    ["address"] = network.WanIPAddress.ToString() + "/24",
                    ["addresses"] = new JsonArray(network.WanIPAddress.ToString()),
                    ["gateways"] = new JsonArray(gateways?.Select(t => JsonValue.Create(t)).ToArray() ?? []),
                    ["mac"] = string.Join(":", network.WanMacAddress.Select(t => t.ToString("x2"))),
                    ["l1up"] = true,
                    ["mtu"] = 1500,
                    ["speed"] = 1000,
                    ["stats"] = new JsonObject
                    {
                        ["rx_bytes"] = wanStats?.BytesReceived ?? 0,
                        ["rx_dropped"] = wanStats?.IncomingPacketsDiscarded ?? 0,
                        ["rx_errors"] = wanStats?.IncomingPacketsWithErrors ?? 0,
                        ["rx_multicast"] = wanStats?.NonUnicastPacketsReceived ?? 0,
                        ["rx_packets"] = wanStats?.UnicastPacketsReceived ?? 0,
                        ["tx_bytes"] = wanStats?.BytesSent ?? 0,
                        ["tx_dropped"] = wanStats?.OutgoingPacketsDiscarded ?? 0,
                        ["tx_errors"] = wanStats?.OutgoingPacketsWithErrors ?? 0,
                        ["tx_packets"] = wanStats?.UnicastPacketsSent ?? 0,
                    },
                    ["up"] = wan?.OperationalStatus == OperationalStatus.Up,
                },
                new JsonObject
                {
                    ["autoneg"] = true,
                    ["duplex"] = "full",
                    ["name"] = "eth1",
                    ["address"] = IPAddress.ToString() + "/24",
                    ["addresses"] = new JsonArray(IPAddress.ToString()),
                    ["l1up"] = true,
                    ["mac"] = MacAddressString,
                    ["mtu"] = 1500,
                    ["speed"] = 1000,
                    ["stats"] = new JsonObject
                    {
                        ["rx_bytes"] = lanStats?.BytesReceived ?? 0,
                        ["rx_dropped"] = lanStats?.IncomingPacketsDiscarded ?? 0,
                        ["rx_errors"] = lanStats?.IncomingPacketsWithErrors ?? 0,
                        ["rx_multicast"] = lanStats?.NonUnicastPacketsReceived ?? 0,
                        ["rx_packets"] = lanStats?.UnicastPacketsReceived ?? 0,
                        ["tx_bytes"] = lanStats?.BytesSent ?? 0,
                        ["tx_dropped"] = lanStats?.OutgoingPacketsDiscarded ?? 0,
                        ["tx_errors"] = lanStats?.OutgoingPacketsWithErrors ?? 0,
                        ["tx_packets"] = lanStats?.UnicastPacketsSent ?? 0,
                    },
                    ["up"] = true,
                }
            );
#pragma warning restore CA1416 // Validate platform compatibility
        }
    }
}
