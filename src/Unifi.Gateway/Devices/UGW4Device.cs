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
    public class UGW4Device : BaseUnifiGatewayDevice
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

        protected override async Task AddExtraInformMessage(JsonObject message)
        {
            message["has_dpi"] = false;
            message["has_vti"] = false;
            message["has_ssh_disable"] = false;
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
            var (_, wanStats) = network.GetWanStatistics();

            message["uplink"] = "eth2";
            message["if_table"] = new JsonArray([
                await GetLanInterface("eth0", 1, IPAddress, Netmask, MacAddress, lanStats),
                await GetDisableInterface("eth1", 2),
                await GetWanInterfaceAsync("eth2", 3, network.WanIPAddress, network.WanNetmask, network.WanMacAddress, wanStats),
                await GetDisableInterface("eth3", 4),
            ]);

            message["pfor-stats"] = new JsonArray([
                new JsonObject()
                {
                    ["id"] = "596add99e4b0a76e35003e00",
                    ["rx_bytes"] = 41444574,
                    ["rx_packets"] = 305634,
                    ["tx_bytes"] = 88048319,
                    ["tx_packets"] = 364768,
                },
            ]);
        }

        private void Log(string direction, byte[] data)
        {
            var message = Encoding.UTF8.GetString(data);
            logger.LogInformation("{direction}: {message}", direction, message);
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
                Log("Response", response = await protocol.SendRequestAsync(InformUrl, Key, request));

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
                Log("Response", response = await protocol.SendRequestAsync(InformUrl, Key, request));

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
                Log("Response", response = await protocol.SendRequestAsync(InformUrl, Key, request));

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
                Log("Response", response = await protocol.SendRequestAsync(InformUrl, Key, request));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Speed test has been failed.");
            }

            static double GetTime() => DateTimeOffset.Now.ToUnixTimeMilliseconds() / 1000;
        }
    }
}
