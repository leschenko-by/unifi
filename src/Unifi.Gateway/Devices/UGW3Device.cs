using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json.Nodes;

namespace Unifi.Gateway.Devices
{
    public class UGW3Device : BaseUnifiGatewayDevice
    {
        public UGW3Device(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            DeviceName = "UGW3";
            DeviceDisplayName = "UniFi Security Gateway";
        }

        protected override async Task AddExtraInformMessage(JsonObject message)
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
            var (_, wanStats) = network.GetWanStatistics();

            message["uplink"] = "eth0";
            message["if_table"] = new JsonArray([
                await GetWanInterfaceAsync("eth0", 1, network.WanIPAddress, network.WanNetmask, network.WanMacAddress, wanStats),
                await GetLanInterface("eth1", 2, IPAddress, Netmask, MacAddress, lanStats),
                await GetDisableInterface("eth2", 2),
            ]);
        }
    }
}
