using System.Text.Json.Serialization;

namespace Unifi.Gateway.Models.V1
{
    public class FirewallConfiguration
    {
        [JsonPropertyName("name")]
        public Dictionary<string, FirewallRuleGroup> Groups { get; set; } = new();
    }
}
