using Microsoft.Extensions.Options;
using System.Net;
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
        private AdoptOptions configuration;

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
            IOptions<GeneralServiceOptions> serviceOptions)
        {
            this.network = network;
            this.configurationReader = configurationReader;
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
            Directory.CreateDirectory("/usr/src/unifi/logs");
            var logFile = Path.Combine("/usr/src/unifi/logs", DateTime.Now.ToString("yyyy-MM-ddTHH-mm-ss") + ".json");
            await File.WriteAllTextAsync(logFile, json.ReplaceLineEndings());
        }

        public string GetInformMessage()
        {
            var message = new JsonObject();
            if (configuration.Adopted == true)
            {
                throw new NotImplementedException();
            }
            else
            {
                message["mac"] = string.Join(":", MacAddress.Select(t => t.ToString("x2")));
                message["ip"] = IPAddress.ToString();
                message["model"] = DeviceName;
                message["model-display"] = DeviceDisplayName;
                message["version"] = Firmware;
            }

            //var message = CreateBaseInform();
            //message["sys_stats"] = GetSysStats();
            //message["system-stats"] = GetSystemStats();
            //if (configuration.Adopted == true)
            //{
            //    message["discovery_response"] = true;
            //    message["state"] = 1;
            //}
            //else
            //{
            //    message["discovery_response"] = false;
            //    message["state"] = 2;
            //}
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
                ["cfgversion"] = configuration.ConfigVersion,
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
        }
    }
}
