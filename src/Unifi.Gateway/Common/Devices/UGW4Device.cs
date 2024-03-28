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
                    ["upload-progress"] = new JsonArray(100, 150, 200),
                    ["download-progress"] = new JsonArray(100, 150, 200),
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
                    ["upload-progress"] = new JsonArray(100, 150, 200),
                    ["download-progress"] = new JsonArray(100, 150, 200),
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
                    ["upload-progress"] = new JsonArray(100, 150, 200),
                    ["download-progress"] = new JsonArray(100, 150, 200),
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
                    ["upload-progress"] = new JsonArray(100, 150, 200),
                    ["download-progress"] = new JsonArray(100, 150, 200),
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

            var lan = network.GetLanStatistics();
            var wan = network.GetWanStatistics();

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
                    ["name"] = "eth3",
                    ["enable"] = false,
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
