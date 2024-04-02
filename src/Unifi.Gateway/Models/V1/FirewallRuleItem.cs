using System.Text.Json.Serialization;

namespace Unifi.Gateway.Models.V1
{
    public class FirewallRuleItem
    {
        [JsonPropertyName("action")]
        public string Action { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("destination")]
        public FirewallRuleDestination? Destination { get; set; }

        [JsonPropertyName("source")]
        public FirewallRuleSource? Source { get; set; }

        [JsonPropertyName("protocol")]
        public string? Protocol { get; set; }
    }
}
