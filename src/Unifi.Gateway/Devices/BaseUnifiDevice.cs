using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Unifi.Gateway.Common.Interfaces;
using Unifi.Gateway.Models;
using Unifi.Gateway.Models.NTop;

namespace Unifi.Gateway.Devices
{
    public abstract class BaseUnifiDevice : IUnifiDevice
    {
        protected readonly INetworkInfoService network;
        protected readonly ISystemInfoService systemInfo;
        protected readonly IServiceProvider serviceProvider;
        private readonly IOptions<GeneralServiceOptions> serviceOptions;
        private readonly IConfigurationReader configurationReader;
        private readonly IConfigurationWriter configurationWriter;
        private readonly IConnectRequest connectRequest;
        private readonly IEthernetInterface discoveryInterface;
        private JsonObject? nextCommand = null;
        protected Configuration configuration;
        private TimeSpan interval = TimeSpan.FromSeconds(10);

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

            serviceOptions = serviceProvider.GetRequiredService<IOptions<GeneralServiceOptions>>();
            discoveryInterface = network.Interfaces[serviceOptions.Value.DiscoveryPortId]
                ?? throw new InvalidOperationException("Network interface is not ready");
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
                        await ApplySystemConfigurationAsync(data);
                    }

                    configuration.Adopted = true;

                    nextCommand = await CreateBaseInformAsync();
                    nextCommand["inform_as_notif"] = true;
                    nextCommand["notif_reason"] = "setparam";
                    nextCommand["connect_request_ip"] = discoveryInterface.IPAddress.ToString();
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

        protected abstract Task ApplySystemConfigurationAsync(InformResponseMessage data);

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
                message["connect_request_ip"] = discoveryInterface.IPAddress.ToString();
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
                ["hash_id"] = Convert.ToHexString(discoveryInterface.MacAddress),
                ["hostname"] = Dns.GetHostName(),
                ["inform_min_interval"] = 5,
                ["internet"] = true,
                ["inform_url"] = InformUrl,
                ["ip"] = discoveryInterface.IPAddress.ToString(),
                ["isolated"] = false,
                ["kernel_version"] = "4.4.153",
                ["locating"] = false,
                ["mac"] = string.Join(":", discoveryInterface.MacAddress.Select(t => t.ToString("x2"))),
                ["manufacturer_id"] = 4,
                ["model"] = DeviceName,
                ["model_display"] = DeviceDisplayName,
                ["netmask"] = discoveryInterface.Netmask.ToString(),
                ["required_version"] = "3.4.1",
                ["selfrun_beacon"] = true,
                ["serial"] = Convert.ToHexString(discoveryInterface.MacAddress),
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
            var eth = network.Interfaces[serviceOptions.Value.DiscoveryPortId]
                ?? throw new InvalidOperationException("Network interface is not ready");

            var macAddress = eth.MacAddress;
            var ipAddress = eth.IPAddress;
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

        protected JsonArray GetConfigPortTable(int[] wanPorts)
        {
            var eths = new List<JsonObject>();
            for (var i = 0; i < network.Interfaces.Count; i++)
            {
                eths.Add(new JsonObject
                {
                    ["ifname"] = "eth" + i,
                    ["name"] = wanPorts.Contains(i) ? "wan" : "lan",
                });
            }
            return new JsonArray(eths.ToArray());
        }

        protected async Task<JsonArray> GetInterfacesAsync(int[] wanPorts)
        {
            var eths = new List<JsonObject>();
            for (var i = 0; i < network.Interfaces.Count; i++)
            {
                var port = i + 1;
                var eth = network.Interfaces[i];

                if (eth is null)
                {
                    eths.Add(await GetDisableInterface("eth" + i, port));
                }
                else if (wanPorts.Contains(i))
                {
                    eths.Add(await GetWanInterfaceAsync("eth" + i, port, eth.IPAddress, eth.Netmask, eth.MacAddress, eth.GetIPStatistics()));
                }
                else
                {
                    eths.Add(await GetLanInterface("eth" + i, port, eth.IPAddress, eth.Netmask, eth.MacAddress, eth.GetIPStatistics()));
                }
            }
            return new JsonArray(eths.ToArray());
        }

        protected async Task<JsonObject> GetDisableInterface(string name, int port)
        {
            await Task.Yield();
            return new JsonObject
            {
                ["name"] = name,
                ["enable"] = false,
                ["num_port"] = port,
            };
        }

        protected async Task<JsonObject> GetLanInterface(string name, int port, IPAddress address, IPAddress netmask, byte[] mac, IPInterfaceStatistics stats)
        {
            await Task.Yield();

            return new JsonObject
            {
                ["full_duplex"] = true,
                ["name"] = name,
                ["enable"] = true,
                ["ip"] = address.ToString(),
                ["mac"] = string.Join(":", mac.Select(t => t.ToString("x2"))),
                ["netmask"] = netmask.ToString(),
                ["up"] = true,
                ["num_port"] = port,
                ["rx_bytes"] = stats.BytesReceived,
                ["rx_dropped"] = stats.IncomingPacketsDiscarded,
                ["rx_errors"] = stats.IncomingPacketsWithErrors,
                ["rx_multicast"] = stats.NonUnicastPacketsReceived,
                ["rx_packets"] = stats.UnicastPacketsReceived,
                ["speed"] = 1000,
                ["tx_bytes"] = stats.BytesSent,
                ["tx_dropped"] = 0,
                ["tx_errors"] = stats.OutgoingPacketsWithErrors,
                ["tx_packets"] = stats.UnicastPacketsSent,
            };
        }

        protected async Task<long> GetLatencyAsync()
        {
            var latency = 0L;
            try
            {
                var ping = new Ping();
                var addresses = await Dns.GetHostAddressesAsync(configuration.EchoServer);
                var echoServer = addresses.FirstOrDefault();
                if (echoServer != null)
                {
                    var reply = await ping.SendPingAsync(echoServer);
                    if (reply != null && reply.Status == IPStatus.Success)
                    {
                        latency = reply.RoundtripTime;
                    }
                }
            }
            catch
            {
                latency = 0L;
            }

            return latency;
        }

        protected async Task<JsonObject> GetWanInterfaceAsync(
            string name, int port, IPAddress address, IPAddress netmask, byte[] mac, IPInterfaceStatistics stats)
        {
            var latency = await GetLatencyAsync();
            var result = await GetLanInterface(name, port, address, netmask, mac, stats);
            result["latency"] = latency;
            result["uptime"] = await systemInfo.GetUptimeAsync();
            //result["speedtest_lastrun"] = DateTimeOffset.Now.ToUnixTimeSeconds();
            //result["speedtest_ping"] = 1;
            //result["speedtest_status"] = "Idle";
            //result["xput_down"] = 50 + new Random().Next(50);
            //result["xput_up"] = 50 + new Random().Next(50);
            return result;
        }

        protected async Task<JsonArray> GetRoutesAsync()
        {
            await Task.Yield();
            var eths = new List<JsonObject>
            {
                new JsonObject
                {
                    ["pfx"] = "127.0.0.0/8",
                    ["nh"] = new JsonArray([
                        new JsonObject()
                        {
                            ["intf"] = "lo",
                            ["t"] = "C>*",
                        }
                    ]),
                }
            };
            for (var i = 0; i < network.Interfaces.Count; i++)
            {
                var eth = network.Interfaces[i];
                if (eth is not null)
                {
                    eths.Add(new JsonObject
                    {
                        ["pfx"] = GetNetwork(eth.IPAddress, eth.Netmask),
                        ["nh"] = new JsonArray([
                            new JsonObject()
                            {
                                ["intf"] = "eth" + i,
                                ["t"] = "C>*",
                            }
                        ]),
                    });
                    if (eth.Gateways.Length > 0)
                    {
                        eths.Add(new JsonObject
                        {
                            ["pfx"] = "0.0.0.0/0",
                            ["nh"] = new JsonArray([
                                new JsonObject()
                                {
                                    ["intf"] = "eth" + i,
                                    ["metric"] = "1/0",
                                    ["t"] = "S>*",
                                }
                            ]),
                        });
                    }
                }
            }

            return new JsonArray(eths.ToArray());
        }

        protected int GetNetworkLength(IPAddress netmask)
        {
            var length = 0;
            foreach (var o in netmask.GetAddressBytes())
            {
                var bits = o;
                while ((bits & 0x80) != 0)
                {
                    length++;
                    bits = (byte)((bits << 1) & 0xff);
                }
            }

            return length;
        }

        protected string GetNetwork(IPAddress address, IPAddress netmask)
        {
            var addr = address.GetAddressBytes();
            var mask = netmask.GetAddressBytes();

            var result = new byte[addr.Length];
            for (var i = 0; i < addr.Length; i++)
            {
                result[i] = (byte)(addr[i] & mask[i]);
            }

            int length = GetNetworkLength(netmask);
            return string.Join(".", result.Select(t => t.ToString())) + "/" + length;
        }

        protected string GetAddress(IPAddress address, IPAddress netmask)
        {
            int length = GetNetworkLength(netmask);
            return address + "/" + length;
        }

        protected async Task<JsonArray> GetNetworkTableAsync()
        {
            NTopResponse? response = null;
            try
            {
                var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse("Basic " + serviceOptions.Value.NTopAuth);

                var json = await client.GetStringAsync(serviceOptions.Value.NTopUri);
                response = JsonSerializer.Deserialize(json, SourceGenerationContext.Default.NTopResponse);
            }
            catch
            {
                response = null;
            }

            var eths = new List<JsonObject>();
            for (var i = 0; i < network.Interfaces.Count; i++)
            {
                var eth = network.Interfaces[i];
                if (eth is null)
                {
                    eths.Add(new JsonObject
                    {
                        ["name"] = "eth" + i,
                        ["autoneg"] = true,
                        ["l1up"] = false,
                        ["up"] = false,
                    });
                }
                else
                {
                    var mac = string.Join(":", eth.MacAddress.Select(t => t.ToString("x2")));

                    var query =
                        from h in response?.Hosts ?? []
                        where !string.IsNullOrEmpty(h.Router) && string.Equals(mac, h.Router.Replace("-", ":"), StringComparison.OrdinalIgnoreCase)
                        group h by h.MacAddress into g
                        select new JsonObject
                        {
                            ["age"] = 0,
                            ["authorized"] = true,
                            ["ip"] = g.Where(t => t.IpVersion == 4).Select(t => t.IpAddress).FirstOrDefault(),
                            ["mac"] = g.Key,
                            ["uptime"] = g.Max(t => t.Duration),
                            ["tx_bytes"] = g.Sum(t => t.BytesSent),
                            ["rx_bytes"] = g.Sum(t => t.BytesReceived),
                            ["tx_packets"] = g.Sum(t => t.PacketsSent),
                            ["rx_packets"] = g.Sum(t => t.PacketsReceived),
                        };
                    var hosts = query.ToArray();

                    string address = GetAddress(eth.IPAddress, eth.Netmask);
                    var stats = eth.GetIPStatistics();
                    eths.Add(new JsonObject
                    {
                        ["address"] = address,
                        ["addresses"] = new JsonArray([address]),
                        ["autoneg"] = true,
                        ["duplex"] = "full",
                        ["gateways"] = new JsonArray(eth.Gateways.Select(t => JsonValue.Create(t.ToString())).ToArray() ?? []),
                        ["l1up"] = true,
                        ["mac"] = mac,
                        ["mtu"] = 1500,
                        ["name"] = "eth" + i,
                        ["nameservers"] = new JsonArray(eth.DnsAddresses.Select(t => JsonValue.Create(t.ToString())).ToArray() ?? []),
                        ["speed"] = 1000,
                        ["stats"] = new JsonObject
                        {
                            ["multicast"] = stats.NonUnicastPacketsReceived,
                            ["rx_bps"] = 0,
                            ["rx_bytes"] = stats.BytesReceived,
                            ["rx_dropped"] = stats.IncomingPacketsDiscarded,
                            ["rx_errors"] = stats.IncomingPacketsWithErrors,
                            ["rx_multicast"] = stats.NonUnicastPacketsReceived,
                            ["rx_packets"] = stats.UnicastPacketsReceived,
                            ["tx_bps"] = 0,
                            ["tx_bytes"] = stats.BytesSent,
                            ["tx_dropped"] = 0,
                            ["tx_errors"] = stats.OutgoingPacketsWithErrors,
                            ["tx_packets"] = stats.UnicastPacketsSent,
                        },
                        ["up"] = true,
                        ["host_table"] = new JsonArray(hosts)
                    });
                }
            }

            return new JsonArray(eths.ToArray());
        }
    }
}
