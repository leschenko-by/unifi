using System.Net.NetworkInformation;
using System.Text;
using System.Text.Json.Nodes;
using Unifi.Gateway.Common.Interfaces;
using Unifi.Gateway.Json;
using Unifi.SpeedTest;

namespace Unifi.Gateway.Common.Devices
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

        protected override async Task ProcessDataAsync(ResponseData data)
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
                    ["num_port"] = 0,
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
                },
                new JsonObject
                {
                    ["full_duplex"] = true,
                    ["name"] = "eth2",
                    ["enable"] = true,
                    ["ip"] = network.WanIPAddress.ToString(),
                    ["mac"] = string.Join(":", network.WanMacAddress.Select(t => t.ToString("x2"))),
                    ["netmask"] = network.WanNetmask.ToString(),
                    ["up"] = wan?.OperationalStatus == OperationalStatus.Up,
                    ["num_port"] = 0,
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
                }
            );
#pragma warning restore CA1416 // Validate platform compatibility
            //message["dpi-stats"] = new JsonArray(new JsonObject
            //{
            //    ["initialized"] = true,
            //    ["mac"] = MacAddressString,
            //    ["stats"] = new JsonObject
            //    {
            //        ["app"] = 5,
            //        ["cat"] = 3,
            //        ["rx_bytes"] = 82297468,
            //        ["rx_packets"] = 57565,
            //        ["tx_bytes"] = 1710174,
            //        ["tx_packets"] = 25324,
            //    }
            //});
            //message["dpi-stats-table"] = new JsonArray(new JsonObject
            //{
            //    ["_id"] = "5aec9b73fc92ac1eb4d8a150",
            //    ["_subid"] = "5e67f0961b24b874966aa014",
            //    ["initialized"] = "1584128269122",
            //    ["by_app"] = new JsonArray(new JsonObject
            //    {
            //        ["app"] = 5,
            //        ["cat"] = 3,
            //        ["rx_bytes"] = 82297468,
            //        ["rx_packets"] = 57565,
            //        ["tx_bytes"] = 1710174,
            //        ["tx_packets"] = 25324,
            //    }),
            //    ["by_cat"] = new JsonArray(new JsonObject
            //    {
            //        ["apps"] = new JsonArray(5),
            //        ["cat"] = 3,
            //        ["rx_bytes"] = 82297468,
            //        ["rx_packets"] = 57565,
            //        ["tx_bytes"] = 1710174,
            //        ["tx_packets"] = 25324,
            //    })
            //}, new JsonObject
            //{
            //    ["_id"] = "5aec9b73fc92ac1eb4d8a150",
            //    ["_subid"] = "5e67f0961b24b874966aa014",
            //    ["initialized"] = "1584128269122",
            //    ["is_ugw"] = true,
            //    ["by_app"] = new JsonArray(new JsonObject
            //    {
            //        ["app"] = 5,
            //        ["cat"] = 3,
            //        ["clients"] = new JsonArray(new JsonObject
            //        {
            //            ["mac"] = "D8-5E-D3-D4-EF-7F".ToLower().Replace("-", ":"),
            //            ["rx_bytes"] = 82297468,
            //            ["rx_packets"] = 57565,
            //            ["tx_bytes"] = 1710174,
            //            ["tx_packets"] = 25324,
            //        }),
            //        ["known_clients"] = 1,
            //        ["rx_bytes"] = 82297468,
            //        ["rx_packets"] = 57565,
            //        ["tx_bytes"] = 1710174,
            //        ["tx_packets"] = 25324,
            //    }),
            //    ["by_cat"] = new JsonArray(new JsonObject
            //    {
            //        ["apps"] = new JsonArray(5),
            //        ["cat"] = 3,
            //        ["rx_bytes"] = 82297468,
            //        ["rx_packets"] = 57565,
            //        ["tx_bytes"] = 1710174,
            //        ["tx_packets"] = 25324,
            //    })
            //});
        }

        private void Log(string direction, byte[] data)
        {
            var message = Encoding.UTF8.GetString(data);
            logger.LogInformation("{direction}: {message}", direction, message);
        }
    }
}
