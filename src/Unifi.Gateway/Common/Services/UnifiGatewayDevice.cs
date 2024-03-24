using Microsoft.Extensions.Options;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Unifi.Gateway.Common.Interfaces;
using Unifi.Gateway.Json;
using Unifi.Gateway.Options;

namespace Unifi.Gateway.Common.Services
{
    public class UnifiGatewayDevice : IUnifiDevice
    {
        private readonly DateTime startTime = DateTime.Now;

        private readonly INetworkInfoService network;
        private readonly IConfigurationReader configurationReader;
        private readonly IConfigurationWriter configurationWriter;
        private Configuration configuration;
        private JsonObject? nextCommand = null;

        public byte[] MacAddress => network.MacAddress;

        public IPAddress IPAddress => network.IPAddress;

        public IPAddress Netmask => network.Netmask;

        public string InformUrl => configuration.InformUrl;

        public byte[] Key => Convert.FromHexString(configuration.Key);

        public string DeviceName { get; }
        public string DeviceDisplayName { get; }
        public string Firmware { get; }

        public UnifiGatewayDevice(
            INetworkInfoService network,
            IConfigurationReader configurationReader,
            IConfigurationWriter configurationWriter,
            IOptions<GeneralServiceOptions> serviceOptions)
        {
            this.network = network;
            this.configurationReader = configurationReader;
            this.configurationWriter = configurationWriter;
            DeviceName = serviceOptions.Value.Device;
            DeviceDisplayName = serviceOptions.Value.DisplayName;
            Firmware = serviceOptions.Value.Firmware;

            configuration = configurationReader.LoadConfiguration();
        }

        public void LoadConfigration()
        {
            configuration = configurationReader.LoadConfiguration();
        }

        public async Task UpdateAsync(string json)
        {
            nextCommand = null;

            var data = JsonSerializer.Deserialize(json, SourceGenerationContext.Default.ResponseData);
            if (data is not null)
            {
                switch (data.Command)
                {
                    case "setparam":
                        if (!string.IsNullOrEmpty(data.MgmtCfg))
                        {
                            var lines = data.MgmtCfg.Split("\n", StringSplitOptions.RemoveEmptyEntries);
                            if (lines.Length > 0)
                            {
                                configuration.MgmtCfg = lines;
                            }
                        }
                        if (!string.IsNullOrEmpty(data.SystemCfg))
                        {
                            var lines = data.SystemCfg.Split("\n", StringSplitOptions.RemoveEmptyEntries);
                            if (lines.Length > 0)
                            {
                                configuration.SystemCfg = lines;
                            }
                        }

                        nextCommand = CreateBaseInform();
                        nextCommand["inform_as_notif"] = true;
                        nextCommand["notif_reason"] = "setparam";
                        nextCommand["notif_payload"] = "";
                        nextCommand["state"] = 0; // DS_ADOPTING
                        if (configuration.Adopted != true)
                        {
                            nextCommand["discovery_response"] = true;
                        }

                        configuration.Adopted = true;
                        break;
                }
            }

            configurationWriter.SaveConfiguration(configuration);
            await Task.CompletedTask;
        }

        public string GetInformMessage()
        {
            if (nextCommand != null)
            {
                return nextCommand.ToString();
            }

            var message = CreateBaseInform();
            message["sys_stats"] = GetSysStats();
            message["system-stats"] = GetSystemStats();
            if (configuration.Adopted != true)
            {
                message["discovery_response"] = true;
                message["state"] = 1; // DS_UNKNOWN
            }
            else
            {
                message["discovery_response"] = false;
                message["state"] = 2; // DS_READY
            }
            return message.ToString();
        }

        private JsonObject GetSysStats() => new JsonObject
        {
            ["loadavg_1"] = 0,
            ["loadavg_5"] = 0,
            ["loadavg_15"] = 0,
            ["mem_buffer"] = 0,
            ["mem_total"] = 1,
            ["mem_used"] = 1,
        };

        private JsonObject GetSystemStats()
        {
            var uptime = (int)(DateTime.Now - startTime).TotalSeconds;

            return new JsonObject
            {
                ["cpu"] = 0,
                ["mem"] = 0.5,
                ["uptime"] = uptime,
            };
        }

        private JsonObject CreateBaseInform()
        {
            var uptime = (int)(DateTime.Now - startTime).TotalSeconds;

            var utcNow = DateTimeOffset.UtcNow;
            var time = utcNow.ToUnixTimeSeconds();
            var uri = new Uri(InformUrl);
            return new JsonObject
            {
                ["fingerprint"] = "b2:5b:e2:98:c3:b1:2e:2e:38:fd:f9:34:b7:72:9e:67",
                ["board_rev"] = 33,
                ["bootid"] = 1,
                ["bootrom_version"] = "unifi-enlarge-buf.-1-g63fe9b5d-dirty",
                ["cfgversion"] = GetConfigVersion(),
                ["default"] = false,
                ["dualboot"] = true,
                ["hash_id"] = Convert.ToHexString(MacAddress),
                ["hostname"] = Dns.GetHostName(),
                ["inform_ip"] = uri.Host,
                ["inform_url"] = InformUrl,
                ["ip"] = IPAddress.ToString(),
                ["isolated"] = false,
                ["kernel_version"] = "4.1.20-ubnt",
                ["locating"] = false,
                ["mac"] = string.Join(":", MacAddress.Select(t => t.ToString("x2"))),
                ["manufacturer_id"] = 4,
                ["model"] = DeviceName,
                ["model_display"] = DeviceDisplayName,
                ["netmask"] = Netmask.ToString(),
                ["required_version"] = "3.4.1",
                ["selfrun_beacon"] = true,
                ["serial"] = Convert.ToHexString(MacAddress),
                ["state"] = 2,
                ["time"] = utcNow.ToUnixTimeSeconds(),
                ["time_ms"] = utcNow.Millisecond,
                ["uptime"] = uptime,
                ["version"] = Firmware,
                ["connect_request_ip"] = IPAddress.ToString(),
                ["connect_request_port"] = 57201,
            };

            string GetConfigVersion()
            {
                if (string.IsNullOrEmpty(configuration.ConfigVersion))
                {
                    var version = configuration.MgmtCfg.FirstOrDefault(t => t.StartsWith("cfgversion="));
                    if (version is not null)
                    {
                        return version.Split("=")[1];
                    }
                }

                return configuration.ConfigVersion;
            }
        }
    }
}
