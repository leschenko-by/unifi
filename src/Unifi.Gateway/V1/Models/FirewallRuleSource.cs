using System.Text.Json.Serialization;

namespace Unifi.Gateway.V1.Models
{
    public class FirewallRuleSource
    {
        [JsonPropertyName("address")]
        public string? Address { get; set; }
    }
}
