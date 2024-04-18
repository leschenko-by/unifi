using System.Text.Json.Serialization;

namespace Unifi.Gateway.V1.Models
{
    public class FirewallRuleDestination
    {
        [JsonPropertyName("address")]
        public string? Address { get; set; }

        [JsonPropertyName("port")]
        public string? Port { get; set; }

        [JsonPropertyName("group")]
        public FirewallRuleDestinationGroup? Group { get; set; }

        [JsonPropertyName("mac-address")]
        public string? MacAddress { get; set; }
    }
}
