using Org.BouncyCastle.Asn1.Pkcs;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Unifi.Gateway.Common.Interfaces;
using Unifi.Gateway.Json;
using Unifi.Gateway.Models;
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
            message["if_table"] = await GetInterfacesAsync([2,3]);
            message["network_table"] = await GetNetworkTableAsync();
            message["routes"] = await GetRoutesAsync();
        }

        private async Task<JsonArray> GetRoutesAsync()
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

        private async Task<JsonArray> GetNetworkTableAsync()
        {
            await Task.Yield();
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
                        ["mac"] = string.Join(":", eth.MacAddress.Select(t => t.ToString("x2"))),
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
                    });
                }
            }

            return new JsonArray(eths.ToArray());
        }

        private static string GetAddress(IPAddress address, IPAddress netmask)
        {
            int length = GetNetworkLength(netmask);
            return address + "/" + length;
        }

        private static string GetNetwork(IPAddress address, IPAddress netmask)
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

        private static int GetNetworkLength(IPAddress netmask)
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
