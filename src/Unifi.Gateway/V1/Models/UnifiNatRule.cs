using System.Text.Json.Serialization;

namespace Unifi.Gateway.V1.Models
{
    public class UnifiNatRule
    {
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("log")]
        public string Log { get; set; } = string.Empty;

        [JsonPropertyName("protocol")]
        public string Protocol { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("outbound-interface")]
        public string OutboundInterface { get; set; } = string.Empty;

        [JsonPropertyName("source")]
        public UnifiNatSource Source { get; set; } = new();
    }
}
