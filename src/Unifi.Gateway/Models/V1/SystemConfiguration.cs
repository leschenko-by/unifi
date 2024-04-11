using System.Text.Json.Serialization;

namespace Unifi.Gateway.Models.V1
{
    public class SystemConfiguration
    {
        [JsonPropertyName("firewall")]
        public FirewallConfiguration Firewall { get; set; } = new();

        [JsonPropertyName("port-forward")]
        public PortForwardConfiguration PortForward { get; set; } = new();

        [JsonPropertyName("unifi")]
        public UnifiSystemConfiguration Unifi { get; set; } = new();

        [JsonPropertyName("service")]
        public UnifiServiceConfiguration Service { get; set; } = new();
    }
}
