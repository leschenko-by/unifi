using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Unifi.Gateway.Common.Interfaces;
using Unifi.Gateway.Models;
using Unifi.Gateway.Models.NTop;
using Unifi.SpeedTest;

namespace Unifi.Gateway.Devices
{
    public class UGW4Device : BaseGatewayV1Device
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

            message["vpn"] = new JsonArray();

            message["config_network_wan"] = JsonSerializer.Deserialize(
                configuration.ConfigNetworkWAN,
                SourceGenerationContext.Default.JsonObject);

            message["config_network_wan2"] = JsonSerializer.Deserialize(
                configuration.ConfigNetworkWAN2,
                SourceGenerationContext.Default.JsonObject);

            message["uplink"] = "eth2";
            message["config_port_table"] = GetConfigPortTable([2, 3]);
            message["if_table"] = await GetInterfacesAsync([2, 3]);
            message["network_table"] = await GetNetworkTableAsync();
            message["routes"] = await GetRoutesAsync();

            try
            {
                var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse("Basic " + serviceOptions.Value.NTopAuth);

                var json = await client.GetStringAsync(serviceOptions.Value.NTopUri);
                var response = JsonSerializer.Deserialize(json, SourceGenerationContext.Default.NTopResponse);

                var query =
                    from h in response?.Hosts ?? []
                    where !string.IsNullOrEmpty(h.Router)
                    group h by h.MacAddress into g
                    select new
                    {
                        IP = g.Where(t => t.IpVersion == 4).Select(t => t.IpAddress).FirstOrDefault(),
                        Mac = g.Key.ToLower(),
                        Duration = g.Max(t => t.Duration),
                        BytesSent = g.Sum(t => t.BytesSent),
                        BytesReceived = g.Sum(t => t.BytesReceived),
                        PacketsSent = g.Sum(t => t.PacketsSent),
                        PacketsReceived = g.Sum(t => t.PacketsReceived),
                    };
                var hosts = query.ToArray();

                message["dpi-clients"] = new JsonArray(hosts.Select(t => JsonValue.Create(t.Mac)).ToArray());
                message["dpi-stats"] = new JsonArray(
                    hosts.Select(t => new JsonObject
                    {
                        ["mac"] = t.Mac,
                        ["initialized"] = "94107792805",
                        ["stats"] = new JsonArray([
                            new JsonObject
                            {
                                ["app"] = 5,
                                ["cat"] = 3,
                                ["rx_bytes"] = t.BytesReceived,
                                ["rx_packets"] = t.PacketsReceived,
                                ["tx_bytes"] = t.BytesSent,
                                ["tx_packets"] = t.PacketsSent,
                            }
                        ])
                    }).ToArray()
                );
            }
            catch
            {
            }
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
