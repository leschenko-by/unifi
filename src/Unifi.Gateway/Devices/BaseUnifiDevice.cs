using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Unifi.Gateway.Common.Interfaces;
using Unifi.Gateway.Json;
using Unifi.Gateway.Models;

namespace Unifi.Gateway.Devices
{
    public abstract class BaseUnifiDevice : IUnifiDevice
    {
        protected readonly INetworkInfoService network;
        protected readonly ISystemInfoService systemInfo;

        private readonly IConfigurationReader configurationReader;
        private readonly IConfigurationWriter configurationWriter;
        private readonly IConnectRequest connectRequest;
        private readonly IServiceProvider serviceProvider;
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
            this.serviceProvider = serviceProvider;
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

            var data = JsonSerializer.Deserialize(json, SourceGenerationContext.Default.InformResponseMessage);
            if (data is not null)
            {
                await ProcessDataAsync(data);
            }
        }

        protected virtual async Task ProcessDataAsync(InformResponseMessage data)
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
                        var configurator = serviceProvider.GetRequiredService<ISystemConfigurationService>();
                        await configurator.ApplyAsync(data.SystemCfg, configuration);
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

            return JsonSerializer.Serialize(message, SourceGenerationContext.Default.JsonObject);
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

            await AddExtraInformMessage(message);
            return message;
        }

        protected abstract Task AddExtraInformMessage(JsonObject message);

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

        public async Task SendDiscoveryAsync(int broadcastIndex)
        {
            var macAddress = network.LanMacAddress;
            var ipAddress = network.LanIPAddress;
            if (ipAddress is null) return;

            var endPoint = new IPEndPoint(IPAddress.Parse("233.89.188.1"), 10001);
            using var client = new UdpClient(new IPEndPoint(ipAddress, 0));

            var datagram = await BuildDatagramAsync(broadcastIndex, macAddress, ipAddress.GetAddressBytes());
            await client.SendAsync(datagram, datagram.Length, endPoint);
        }

        private async Task<byte[]> BuildDatagramAsync(int broadcastIndex, byte[] macAddress, byte[] ipAddress)
        {
            var uptime = await systemInfo.GetUptimeAsync();

            var device = DeviceName;
            var firmware = "4.4.44.5213871";

            var builder = new UnifyDatagramBuilder();
            builder.Add(1, macAddress);
            builder.Add(2, [.. macAddress, .. ipAddress]);
            builder.Add(3, Encoding.ASCII.GetBytes($"{device}.v{firmware}"));
            builder.Add(10, BitConverter.GetBytes(uptime).Reverse().ToArray());
            builder.Add(11, Encoding.ASCII.GetBytes("UBNT"));
            builder.Add(12, Encoding.ASCII.GetBytes(device));
            builder.Add(19, macAddress);
            builder.Add(18, BitConverter.GetBytes(broadcastIndex).Reverse().ToArray());
            builder.Add(21, Encoding.ASCII.GetBytes(device));
            builder.Add(27, Encoding.ASCII.GetBytes(firmware));
            builder.Add(22, Encoding.ASCII.GetBytes(firmware));

            return builder.Build();
        }
    }
}
