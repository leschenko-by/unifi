using Microsoft.Extensions.Options;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Unifi.Gateway.Common.Interfaces;
using Unifi.Gateway.Json;
using Unifi.Gateway.Options;

namespace Unifi.Gateway.Common.Devices
{
    public abstract class UnifiBaseDevice : IUnifiDevice
    {
        protected readonly DateTime startTime = DateTime.Now;

        protected readonly INetworkInfoService network;
        private readonly IConfigurationReader configurationReader;
        private readonly IConfigurationWriter configurationWriter;
        private JsonObject? nextCommand = null;
        protected Configuration configuration;

        public byte[] MacAddress => network.MacAddress;

        public string MacAddressString => string.Join(":", MacAddress.Select(t => t.ToString("x2")));

        public IPAddress IPAddress => network.IPAddress;

        public IPAddress Netmask => network.Netmask;

        public string InformUrl => configuration.InformUrl;

        public byte[] Key => Convert.FromHexString(configuration.Key);

        public string DeviceName { get; }
        public string DeviceDisplayName { get; }
        public string Firmware { get; }

        public UnifiBaseDevice(
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

        public void SaveConfigration()
        {
            configurationWriter.SaveConfiguration(configuration);
        }

        public async Task ParseResponseAsync(string json)
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
                            Directory.CreateDirectory("/etc/unifi");
                            File.WriteAllText("/etc/unifi/system.json", data.SystemCfg);
                        }

                        configuration.Adopted = true;

                        nextCommand = CreateBaseInform();
                        nextCommand["inform_as_notif"] = true;
                        nextCommand["notif_reason"] = "setparam";
                        nextCommand["connect_request_ip"] = IPAddress.ToString();
                        nextCommand["connect_request_port"] = "52884";
                        break;
                }
            }

            await Task.CompletedTask;
        }

        public string GetInformMessage()
        {
            if (nextCommand != null)
            {
                return nextCommand.ToString();
            }

            var message = CerateInformMessage();
            if (configuration.Adopted != true)
            {
                message["fingerprint"] = configuration.Fingerprint;
                message["discovery_response"] = true;
                message["state"] = 1; // DS_UNKNOWN
            }

            return message.ToString();
        }

        private JsonObject CerateInformMessage()
        {
            var message = CreateBaseInform();
            message["sys_stats"] = GetSysStats();
            message["system-stats"] = GetSystemStats();
            if (configuration.Adopted)
            {
                message["connect_request_ip"] = IPAddress.ToString();
                message["connect_request_port"] = "52884";
            }

            AddExtraInformMessage(message);
            return message;
        }

        protected abstract void AddExtraInformMessage(JsonObject message);

        private JsonObject GetSysStats() => new JsonObject
        {
            ["loadavg_1"] = "0.09",
            ["loadavg_5"] = "0.16",
            ["loadavg_15"] = "0.08",
            ["mem_buffer"] = 0,
            ["mem_total"] = 128593920,
            ["mem_used"] = 50077696,
        };

        private JsonObject GetSystemStats()
        {
            var uptime = (int)(DateTime.Now - startTime).TotalSeconds;

            return new JsonObject
            {
                ["cpu"] = "5.2",
                ["mem"] = "38.8",
                ["uptime"] = uptime.ToString(),
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
                ["architecture"] = "mips",
                ["board_rev"] = 33,
                ["bootid"] = 0,
                ["bootrom_version"] = "unifi-enlarge-buf.-1-g63fe9b5d-dirty",
                ["cfgversion"] = GetConfigVersion(),
                ["default"] = false,
                ["dualboot"] = true,
                ["hash_id"] = Convert.ToHexString(MacAddress),
                ["hostname"] = Dns.GetHostName(),
                ["inform_min_interval"] = 5,
                ["inform_url"] = InformUrl,
                ["ip"] = IPAddress.ToString(),
                ["isolated"] = false,
                ["kernel_version"] = "4.4.153",
                ["locating"] = false,
                ["mac"] = MacAddressString,
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
                ["tm_ready"] = true,
                ["uptime"] = uptime,
                ["version"] = Firmware,
                ["upgrade_duration"] = 150,
                ["reboot_duration"] = 30,
                ["state"] = 2, // DS_READY
                ["discovery_response"] = false,
                ["has_crash_logs"] = false,
                ["sys_error_caps"] = 0,
                ["ipv4_active_leases"] = new JsonArray(),
            };

            string GetConfigVersion()
            {
                var version = configuration.MgmtCfg.FirstOrDefault(t => t.StartsWith("cfgversion="));
                if (version is not null)
                {
                    return version.Split("=")[1];
                }

                return "?";
            }
        }
    }
}
