using System.Text.Json.Serialization;

namespace Unifi.Gateway.V1.Models
{
    public class FirewallRuleGroup
    {
        [JsonPropertyName("default-action")]
        public string DefaultAction { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("rule")]
        public Dictionary<string, FirewallRule> Rules { get; set; } = new();
    }
}
