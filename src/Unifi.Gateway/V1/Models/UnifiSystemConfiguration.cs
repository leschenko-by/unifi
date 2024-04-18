using System.Text.Json.Serialization;

namespace Unifi.Gateway.V1.Models
{
    public class UnifiSystemConfiguration
    {
        [JsonPropertyName("echo_server")]
        public string EchoServer { get; set; } = string.Empty;

        [JsonPropertyName("config_network_wan")]
        public string ConfigNetworkWAN { get; set; } = string.Empty;

        [JsonPropertyName("config_network_wan2")]
        public string ConfigNetworkWAN2 { get; set; } = string.Empty;

        [JsonPropertyName("offload_pfor")]
        public UnifiPortForward PortForward { get; set; } = new();
    }
}
