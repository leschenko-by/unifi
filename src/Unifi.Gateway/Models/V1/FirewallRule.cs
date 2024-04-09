using System.Text.Json.Serialization;

namespace Unifi.Gateway.Models.V1
{
    public class FirewallRule
    {
        [JsonPropertyName("action")]
        public string Action { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("destination")]
        public FirewallRuleDestination? Destination { get; set; }

        [JsonPropertyName("source")]
        public FirewallRuleDestination? Source { get; set; }

        [JsonPropertyName("protocol")]
        public string? Protocol { get; set; }

        [JsonPropertyName("state")]
        public FirewallRuleState? State { get; set; }
    }
}
