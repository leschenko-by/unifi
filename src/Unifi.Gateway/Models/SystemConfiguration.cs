using System.Text.Json.Serialization;

namespace Unifi.Gateway.Models
{
    public class SystemConfiguration
    {
        [JsonPropertyName("port-forward")]
        public PortForwardConfiguration PortForward { get; set; } = new();

        [JsonPropertyName("unifi")]
        public UnifiSystemConfiguration Unifi { get; set; } = new();
    }

    public class UnifiSystemConfiguration
    {
        [JsonPropertyName("echo_server")]
        public string EchoServer { get; set; } = string.Empty;

        [JsonPropertyName("config_network_wan")]
        public string ConfigNetworkWAN { get; set; } = string.Empty;

        [JsonPropertyName("config_network_wan2")]
        public string ConfigNetworkWAN2 { get; set; } = string.Empty;
    }

    public class PortForwardConfiguration
    {
        [JsonPropertyName("rule")]
        public Dictionary<string, PortForwardRule> Rules { get; set; } = new();
    }

    public class PortForwardRule
    {
        [JsonPropertyName("forward-to")]
        public ForwardDestination Destination { get; set; } = new();

        [JsonPropertyName("original-port")]
        public string OriginalPort { get; set; } = string.Empty;

        [JsonPropertyName("protocol")]
        public string Protocol { get; set; } = string.Empty;
    }

    public class ForwardDestination
    {
        [JsonPropertyName("address")]
        public string Address { get; set; } = string.Empty;

        [JsonPropertyName("port")]
        public string? Port { get; set; }
    }
}
