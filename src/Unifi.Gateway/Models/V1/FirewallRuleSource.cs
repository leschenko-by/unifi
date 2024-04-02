using System.Text.Json.Serialization;

namespace Unifi.Gateway.Models.V1
{
    public class FirewallRuleSource
    {
        [JsonPropertyName("address")]
        public string? Address { get; set; }
    }
}
