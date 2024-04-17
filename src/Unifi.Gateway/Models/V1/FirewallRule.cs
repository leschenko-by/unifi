using System.Text.Json.Nodes;
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

        [JsonPropertyName("icmp")]
        public FirewallRuleIcmp? Icmp { get; set; }

        [JsonPropertyName("icmpv6")]
        public FirewallRuleIcmpV6? Icmpv6 { get; set; }

        [JsonPropertyName("log")]
        public string? Log { get; set; }

        [JsonPropertyName("ipsec")]
        public JsonNode? Ipsec { get; set; }
    }
}
