using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json.Nodes;
using Unifi.Gateway.Common.Interfaces;
using Unifi.Gateway.Models;
using Unifi.SpeedTest;

namespace Unifi.Gateway.Devices
{
    public class UGW4Device : BaseUnifiDevice
    {
        private readonly IUnifiProtocol protocol;
        private readonly ILogger<UGW4Device> logger;

        public UGW4Device(IServiceProvider serviceProvider, IUnifiProtocol protocol, ILogger<UGW4Device> logger)
            : base(serviceProvider)
        {
            this.protocol = protocol;
            this.logger = logger;

            DeviceName = "UGW4";
            DeviceDisplayName = "UniFi Security Gateway-Pro";
        }

        protected override async Task ProcessDataAsync(InformResponseMessage data)
        {
            switch (data.Type)
            {
                case "cmd":
                    switch (data.Command)
                    {
                        case "speed-test":
                            RunSpeedTest();
                            return;
                    }
                    Interval = TimeSpan.FromSeconds(1);
                    break;
            }
            await base.ProcessDataAsync(data);

            async void RunSpeedTest()
            {
                await Task.Run(StartSpeedTest);
            }
        }

        private async Task StartSpeedTest()
        {
            byte[] request;
            byte[] response;
            try
            {
                logger.LogInformation("Starting speed test");

                var message = await CreateBaseInformAsync();
                message["sys_stats"] = await GetSysStats();
                message["system-stats"] = await GetSystemStats();
                message["speedtest-status"] = new JsonObject
                {
                    ["latency"] = 0,
                    ["rundate"] = GetTime(),
                    ["runtime"] = GetTime(),
                    ["status_download"] = 0,
                    ["status_ping"] = 11,
                    ["status_summary"] = 1,
                    ["status_upload"] = 0,
                    ["xput_download"] = 0,
                    ["xput_upload"] = 0,
                };
                Log("Request", request = Encoding.UTF8.GetBytes(message.ToString()));
                Log("Response", response = await protocol.SendRequestAsync(InformUrl, Key, request, default));

                var client = new SpeedTestClient();
                var server = await client.GetServerAsync();

                var latency = await client.TestServerLatencyAsync(server);
                message["speedtest-status"] = new JsonObject
                {
                    ["latency"] = latency,
                    ["rundate"] = GetTime(),
                    ["runtime"] = GetTime(),
                    ["status_download"] = 1,
                    ["status_ping"] = 2,
                    ["status_summary"] = 1,
                    ["status_upload"] = 0,
                    ["xput_download"] = 0,
                    ["xput_upload"] = 0,
                };
                Log("Request", request = Encoding.UTF8.GetBytes(message.ToString()));
                Log("Response", response = await protocol.SendRequestAsync(InformUrl, Key, request, default));

                var download = await client.TestDownloadSpeedAsync(server, 16);

                message["speedtest-status"] = new JsonObject
                {
                    ["latency"] = latency,
                    ["rundate"] = GetTime(),
                    ["runtime"] = GetTime(),
                    ["status_download"] = 2,
                    ["status_ping"] = 2,
                    ["status_summary"] = 1,
                    ["status_upload"] = 2,
                    ["xput_download"] = download / 1024,
                    ["xput_upload"] = 0,
                };
                Log("Request", request = Encoding.UTF8.GetBytes(message.ToString()));
                Log("Response", response = await protocol.SendRequestAsync(InformUrl, Key, request, default));

                var upload = await client.TestUploadSpeedAsync(server, 16);

                message["speedtest-status"] = new JsonObject
                {
                    ["latency"] = latency,
                    ["rundate"] = GetTime(),
                    ["runtime"] = GetTime(),
                    ["status_download"] = 2,
                    ["status_ping"] = 2,
                    ["status_summary"] = 2,
                    ["status_upload"] = 2,
                    ["xput_download"] = download / 1024,
                    ["xput_upload"] = upload / 1024,
                };
                Log("Request", request = Encoding.UTF8.GetBytes(message.ToString()));
                Log("Response", response = await protocol.SendRequestAsync(InformUrl, Key, request, default));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Speed test has been failed.");
            }

            static double GetTime() => DateTimeOffset.Now.ToUnixTimeMilliseconds() / 1000;
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
                    ["name"] = "lan",
                },
                new JsonObject
                {
                    ["ifname"] = "eth1",
                    ["name"] = "lan2",
                },
                new JsonObject
                {
                    ["ifname"] = "eth2",
                    ["name"] = "wan",
                },
                new JsonObject
                {
                    ["ifname"] = "eth3",
                    ["name"] = "wan2",
                }
            );

            var (_, lanStats) = network.GetLanStatistics();
            var (wan, wanStats) = network.GetWanStatistics();
            var gateways = wan?.GetIPProperties().GatewayAddresses
                .Select(gateway => gateway.Address)
                .Where(t => t.AddressFamily == AddressFamily.InterNetwork)
                .ToArray();

            message["routes"] = new JsonArray(
                new JsonObject
                {
                    ["pfx"] = "0.0.0.0/0",
                    ["nh"] = new JsonArray(
                        new JsonObject
                        {
                            ["intf"] = "eth2",
                            ["metric"] = "1/0",
                            ["t"] = "S>*",
                            ["via"] = gateways?.FirstOrDefault()?.ToString() ?? "0.0.0.0",
                        }
                    ),
                },
                new JsonObject
                {
                    ["pfx"] = GetNetwork(network.WanIPAddress, network.WanNetmask),
                    ["nh"] = new JsonArray(
                        new JsonObject
                        {
                            ["intf"] = "eth2",
                            ["t"] = "C>*",
                        }
                    ),
                },
                new JsonObject
                {
                    ["pfx"] = GetNetwork(network.LanIPAddress, network.LanNetmask),
                    ["nh"] = new JsonArray(
                        new JsonObject
                        {
                            ["intf"] = "eth0",
                            ["t"] = "C>*",
                        }
                    ),
                }
            );

            static string GetNetwork(IPAddress address, IPAddress netmask)
            {
                var bytes = address.GetAddressBytes();
                var mask = netmask.GetAddressBytes();
                var network = bytes.Zip(mask, (a, b) => (byte)(a & b)).ToArray();
                var length = 0;
                foreach (var m in mask)
                {
                    var b = m;
                    while ((b & 128) != 0)
                    {
                        length++;
                        b = (byte)((b << 1) & 255);
                    }
                }
                return string.Join(".", network.Select(t=>t.ToString())) + "/" + length;
            }

#pragma warning disable CA1416 // Validate platform compatibility
            message["if_table"] = new JsonArray(
                new JsonObject
                {
                    ["full_duplex"] = true,
                    ["name"] = "eth0",
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
                    ["name"] = "eth1",
                    ["enable"] = false,
                    ["num_port"] = 2,
                },
                new JsonObject
                {
                    ["full_duplex"] = true,
                    ["name"] = "eth2",
                    ["enable"] = true,
                    ["ip"] = network.WanIPAddress.ToString(),
                    ["gateways"] = new JsonArray(gateways?.Select(t => JsonValue.Create(t.ToString())).ToArray() ?? []),
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
                    ["name"] = "eth3",
                    ["enable"] = false,
                    ["num_port"] = 4,
                }
            );
            message["network_table"] = new JsonArray(
                new JsonObject
                {
                    ["autoneg"] = true,
                    ["duplex"] = "full",
                    ["name"] = "eth0",
                    ["address"] = IPAddress.ToString(),
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
                },
                new JsonObject
                {
                    ["autoneg"] = true,
                    ["duplex"] = "full",
                    ["name"] = "eth2",
                    ["address"] = network.WanIPAddress.ToString(),
                    ["addresses"] = new JsonArray(network.WanIPAddress.ToString()),
                    ["gateways"] = new JsonArray(gateways?.Select(t => JsonValue.Create(t.ToString())).ToArray() ?? []),
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
                    ["host_table"] = new JsonArray([
                        new JsonObject
                        {
                            ["age"] = 45,
                            ["authorized"] = true,
                            ["mac"] = "f0:9f:c2:09:2b:f3",
                            ["bc_bytes"] = 5000,
                            ["bc_packets"] = 3000,
                            ["mc_bytes"] = 6000,
                            ["mc_packets"] = 1000,
                            ["rx_bytes"] = 332432,
                            ["rx_packets"] = 2342,
                            ["tx_bytes"] = 234244,
                            ["tx_packets"] = 2343,
                            ["uptime"] = 645635,
                        }
                    ])
                }
            );
#pragma warning restore CA1416 // Validate platform compatibility
        }


        private void Log(string direction, byte[] data)
        {
            var message = Encoding.UTF8.GetString(data);
            logger.LogInformation("{direction}: {message}", direction, message);
        }
    }
}
