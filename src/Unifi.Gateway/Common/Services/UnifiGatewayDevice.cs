using Microsoft.Extensions.Options;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Nodes;
using Unifi.Gateway.Common.Interfaces;
using Unifi.Gateway.Json;
using Unifi.Gateway.Options;

namespace Unifi.Gateway.Common.Services
{
    public class UnifiGatewayDevice : IUnifiDevice
    {
        private readonly NetworkInterface network;
        private readonly IOptions<GeneralServiceOptions> serviceOptions;
        private AdoptOptions? options;

        public byte[] MacAddress { get; }

        public string InformUrl => options?.InformUrl ?? string.Empty;

        public byte[] Key => Convert.FromHexString(options?.Key ?? string.Empty);

        public UnifiGatewayDevice(IOptions<GeneralServiceOptions> serviceOptions)
        {
            var networks = NetworkInterface.GetAllNetworkInterfaces();
            network = networks.First(network => network.Id == serviceOptions.Value.NetworkId);

            MacAddress = network.GetPhysicalAddress().GetAddressBytes();
            this.serviceOptions = serviceOptions;
        }

        public async Task ReloadConfigsAsync()
        {
            if (File.Exists("/etc/unifi/config.json"))
            {
                var json = await File.ReadAllTextAsync("/etc/unifi/config.json");
                options = JsonSerializer.Deserialize(json, SourceGenerationContext.Default.AdoptOptions);
            }
        }

        public async Task UpdateAsync(string json)
        {
            Directory.CreateDirectory("/usr/src/unifi/logs");
            var logFile = Path.Combine("/usr/src/unifi/logs", DateTime.Now.ToString("yyyy-MM-ddTHH-mm-ss") + ".json");
            await File.WriteAllTextAsync(logFile, json.ReplaceLineEndings());
        }

        public string GetInformMessage()
        {
            var message = CerateInformMessage();
            if (options?.Adopted == true)
            {
                message["discovery_response"] = true;
                message["state"] = 1;
            }
            else
            {
                message["discovery_response"] = false;
                message["state"] = 2;
            }
            return message.ToString();
        }

        private JsonObject CerateInformMessage()
        {
            var result = CreateBaseInform();
            result["sys_stats"] = GetSysStats();
            result["system-stats"] = GetSystemStats();

            return result;
        }

        private static JsonObject GetSysStats() => new JsonObject
        {
            ["loadavg_1"] = 0,
            ["loadavg_5"] = 0,
            ["loadavg_15"] = 0,
            ["mem_buffer"] = 0,
            ["mem_total"] = 1,
            ["mem_used"] = 1,
        };

        private static JsonObject GetSystemStats() => new JsonObject
        {
            ["cpu"] = 0,
            ["mem"] = 0,
            ["uptime"] = 0,
        };

        private JsonObject CreateBaseInform()
        {
            var uri = new Uri(InformUrl);
            return new JsonObject
            {
                ["fingerprint"] = "b2:5b:e2:98:c3:b1:2e:2e:38:fd:f9:34:b7:72:9e:67",
                ["board_rev"] = 33,
                ["bootid"] = 1,
                ["bootrom_version"] = "unifi-enlarge-buf.-1-g63fe9b5d-dirty",
                ["cfgversion"] = options?.ConfigVersion,
                ["default"] = false,
                ["dualboot"] = true,
                ["hash_id"] = Convert.ToHexString(MacAddress),
                ["hostname"] = Dns.GetHostName(),
                ["inform_ip"] = uri.Host,
                ["inform_url"] = InformUrl,
                ["ip"] = GetIPAddress().ToString(),
                ["isolated"] = false,
                ["kernel_version"] = "4.1.20-ubnt",
                ["locating"] = false,
                ["mac"] = string.Join(":", MacAddress.Select(t => t.ToString("x2"))),
                ["serial"] = Convert.ToHexString(MacAddress),
                ["manufacturer_id"] = 4,
                ["model"] = serviceOptions.Value.Device,
                ["model_display"] = serviceOptions.Value.DisplayName,
                ["version"] = serviceOptions.Value.Firmware,
                ["connect_request_ip"] = GetIPAddress().ToString(),
                ["connect_request_port"] = 57201,
                ["required_version"] = "4.0.0",
                ["state"] = 2,
            };
        }

        private IPAddress GetIPAddress() =>
            (from address in network.GetIPProperties().UnicastAddresses
             where address.Address.AddressFamily == AddressFamily.InterNetwork
             select address.Address).First();
    }
}
