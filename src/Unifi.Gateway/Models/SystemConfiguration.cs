using System.Text.Json.Serialization;

namespace Unifi.Gateway.Models
{
    public class SystemConfiguration
    {
        [JsonPropertyName("unifi")]
        public UnifiConfiguration Unifi { get; set; } = new UnifiConfiguration();
    }

    public class UnifiConfiguration
    {
        [JsonPropertyName("offload_pfor")]
        public PortForwardingConfiguration PortForwarding { get; set; } = new PortForwardingConfiguration();
    }

    public class PortForwardingConfiguration
    {
        [JsonPropertyName("pfor.status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("pfor.rules")]
        public Dictionary<string, string> Rules { get; set; } = new();
    }
}
