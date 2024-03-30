using System.Net;
using System.Net.NetworkInformation;
using System.Text.Json;
using System.Text.Json.Nodes;
using Unifi.Gateway.Json;

namespace Unifi.Gateway.Devices
{
    public abstract class BaseUnifiGatewayDevice(IServiceProvider serviceProvider) : BaseUnifiDevice(serviceProvider)
    {
        protected override async Task AddExtraInformMessage(JsonObject message)
        {
            await Task.Yield();

            message["vpn"] = new JsonArray();

            message["config_network_wan"] = JsonSerializer.Deserialize(
                configuration.ConfigNetworkWAN,
                SourceGenerationContext.Default.JsonObject);

            message["config_network_wan2"] = JsonSerializer.Deserialize(
                configuration.ConfigNetworkWAN2,
                SourceGenerationContext.Default.JsonObject);
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

        protected async Task<JsonObject> GetWanInterfaceAsync(
            string name, int port, IPAddress address, IPAddress netmask, byte[] mac, IPInterfaceStatistics stats)
        {
            var latency = await GetLatencyAsync();
            var result = await GetLanInterface(name, port, address, netmask, mac, stats);
            result["latency"] = latency;
            result["uptime"] = await systemInfo.GetUptimeAsync();
            result["ip_v6"] = "2a02:bf0:6:10::34";
            //result["speedtest_lastrun"] = DateTimeOffset.Now.ToUnixTimeSeconds();
            //result["speedtest_ping"] = 1;
            //result["speedtest_status"] = "Idle";
            //result["xput_down"] = 50 + new Random().Next(50);
            //result["xput_up"] = 50 + new Random().Next(50);
            return result;
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
    }
}
