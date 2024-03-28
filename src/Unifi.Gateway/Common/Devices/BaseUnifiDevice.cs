using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Unifi.Gateway.Common.Interfaces;
using Unifi.Gateway.Json;

namespace Unifi.Gateway.Common.Devices
{
    public abstract class BaseUnifiDevice : IUnifiDevice
    {
        private readonly ISystemInfoService systemInfo;
        protected readonly INetworkInfoService network;
        private readonly IConfigurationReader configurationReader;
        private readonly IConfigurationWriter configurationWriter;
        private readonly IConnectRequest connectRequest;
        private JsonObject? nextCommand = null;
        protected Configuration configuration;
        private TimeSpan interval = TimeSpan.FromSeconds(10);

        public byte[] MacAddress => network.LanMacAddress;

        public string MacAddressString => string.Join(":", MacAddress.Select(t => t.ToString("x2")));

        public IPAddress IPAddress => network.LanIPAddress;

        public IPAddress Netmask => network.LanNetmask;

        public string InformUrl => configuration.InformUrl;

        public byte[] Key => Convert.FromHexString(configuration.Key);

        public string DeviceName { get; protected set; } = string.Empty;
        public string DeviceDisplayName { get; protected set; } = string.Empty;

        public bool Immediate { get; set; }

        public TimeSpan Interval
        {
            get
            {
                if (Immediate)
                {
                    Immediate = false;
                    return TimeSpan.FromSeconds(0.1);
                }
                return interval;
            }
            set
            {
                interval = value;
            }
        }

        public BaseUnifiDevice(IServiceProvider serviceProvider)
        {
            systemInfo = serviceProvider.GetRequiredService<ISystemInfoService>();
            network = serviceProvider.GetRequiredService<INetworkInfoService>();
            configurationReader = serviceProvider.GetRequiredService<IConfigurationReader>();
            configurationWriter = serviceProvider.GetRequiredService<IConfigurationWriter>();
            connectRequest = serviceProvider.GetRequiredService<IConnectRequest>();
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
                await ProcessDataAsync(data);
            }
        }

        protected virtual async Task ProcessDataAsync(ResponseData data)
        {
            switch (data.Type)
            {
                case "setdefault":
                    configuration.Adopted = false;
                    configuration.InformUrl = string.Empty;
                    configuration.Key = string.Empty;
                    configuration.MgmtCfg = [];
                    break;
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

                    nextCommand = await CreateBaseInformAsync();
                    nextCommand["inform_as_notif"] = true;
                    nextCommand["notif_reason"] = "setparam";
                    nextCommand["connect_request_ip"] = IPAddress.ToString();
                    nextCommand["connect_request_port"] = connectRequest.Port.ToString();

                    Interval = TimeSpan.FromSeconds(1);
                    break;
                case "reboot":
                    Interval = TimeSpan.FromSeconds(1);
                    break;
                case "noop":
                    if (data.Immediate != null)
                    {
                        Immediate = true;
                    }
                    if (data.Interval != null)
                    {
                        Interval = TimeSpan.FromSeconds(data.Interval.Value);
                    }
                    break;
                case "upgrade":
                    if (!string.IsNullOrEmpty(data.Firmware))
                    {
                        configuration.Firmware = data.Firmware;
                    }
                    break;
            }
        }

        public async Task<string> GetInformMessageAsync()
        {
            if (nextCommand != null)
            {
                return nextCommand.ToString();
            }

            var message = await CerateInformMessageAsync();
            if (configuration.Adopted != true)
            {
                message["fingerprint"] = configuration.Fingerprint;
                message["discovery_response"] = true;
                message["state"] = 1; // DS_UNKNOWN
            }

            return message.ToString();
        }

        private async Task<JsonObject> CerateInformMessageAsync()
        {
            var message = await CreateBaseInformAsync();
            message["sys_stats"] = await GetSysStats();
            message["system-stats"] = await GetSystemStats();
            if (configuration.Adopted)
            {
                message["connect_request_ip"] = IPAddress.ToString();
                message["connect_request_port"] = connectRequest.Port.ToString();
            }

            AddExtraInformMessage(message);
            return message;
        }

        protected abstract void AddExtraInformMessage(JsonObject message);

        protected async Task<JsonObject> GetSysStats()
        {
            var totalMem = await systemInfo.GetTotalMemoryAsync();
            var usedMem = await systemInfo.GetUsedMemoryAsync();

            return new JsonObject
            {
                ["loadavg_1"] = "0.09",
                ["loadavg_5"] = "0.16",
                ["loadavg_15"] = "0.08",
                ["mem_buffer"] = 0,
                ["mem_total"] = totalMem,
                ["mem_used"] = Math.Min(usedMem, totalMem),
            };
        }

        protected async Task<JsonObject> GetSystemStats()
        {
            var totalMem = await systemInfo.GetTotalMemoryAsync();
            var usedMem = await systemInfo.GetUsedMemoryAsync();

            return new JsonObject
            {
                ["cpu"] = (await systemInfo.GetCpuUsageAsync()).ToString(),
                ["mem"] = totalMem > 0 ? (100 * Math.Min(usedMem, totalMem) / totalMem).ToString() : "0",
                ["uptime"] = (await systemInfo.GetUptimeAsync()).ToString(),
            };
        }

        protected async Task<JsonObject> CreateBaseInformAsync()
        {
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
                ["time"] = utcNow.ToUnixTimeSeconds(),
                ["time_ms"] = utcNow.Millisecond,
                ["tm_ready"] = true,
                ["uptime"] = await systemInfo.GetUptimeAsync(),
                ["version"] = configuration.Firmware,
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
