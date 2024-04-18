using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Unifi.Gateway.Common.Interfaces;
using Unifi.Gateway.Common.Models;

namespace Unifi.Gateway.V1.Devices
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
            message["has_vti"] = true;
            message["has_ssh_disable"] = true;
            message["fw_caps"] = 3;
            message["guest_token"] = "4C1D46707239C6EB5A2366F505A44A91";
            message["has_default_route_distance"] = true;
            message["has_dnsmasq_hostfile_update"] = false;

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
        }

        private async Task StartSpeedTest()
        {
            using var scope = serviceProvider.CreateScope();
            var client = scope.ServiceProvider.GetRequiredService<ISpeedTestService>();

            byte[] request;
            byte[] response;
            try
            {
                logger.LogInformation("Starting speed test");

                var message = await CreateBaseInformAsync();
                message["sys_stats"] = await GetSysStats();
                message["system-stats"] = await GetSystemStats();

                var status = new JsonObject
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
                message["speedtest-status"] = status;
                request = Encoding.UTF8.GetBytes(message.ToString());
                response = await protocol.SendRequestAsync(InformUrl, Key, request);

                var server = await client.GetServerAsync();

                var latency = await client.TestServerLatencyAsync(server);
                logger.LogInformation("Latency: {latency} ms", latency);

                status["latency"] = latency;
                status["rundate"] = GetTime();
                status["runtime"] = GetTime();
                status["status_download"] = 1;
                status["status_ping"] = 2;
                status["status_summary"] = 1;
                request = Encoding.UTF8.GetBytes(message.ToString());
                response = await protocol.SendRequestAsync(InformUrl, Key, request);

                var download = await client.TestDownloadSpeedAsync(server, 16);
                logger.LogInformation("Download speed: {download}", download / 1024);

                status["rundate"] = GetTime();
                status["runtime"] = GetTime();
                status["status_download"] = 2;
                status["status_upload"] = 2;
                status["xput_download"] = download / 1024;
                request = Encoding.UTF8.GetBytes(message.ToString());
                response = await protocol.SendRequestAsync(InformUrl, Key, request);

                var upload = await client.TestUploadSpeedAsync(server, 16);
                logger.LogInformation("Upload speed: {upload}", upload / 1024);

                status["rundate"] = GetTime();
                status["runtime"] = GetTime();
                status["status_summary"] = 2;
                status["xput_download"] = download / 1024;
                status["xput_upload"] = upload / 1024;
                request = Encoding.UTF8.GetBytes(message.ToString());
                response = await protocol.SendRequestAsync(InformUrl, Key, request);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Speed test has been failed.");
            }

            static double GetTime() => DateTimeOffset.Now.ToUnixTimeMilliseconds() / 1000;
        }
    }
}
