using System.Text.Json.Serialization;

namespace Unifi.Gateway.Models.V1
{
    public class FirewallRuleDestination
    {
        [JsonPropertyName("address")]
        public string? Address { get; set; }

        [JsonPropertyName("port")]
        public string? Port { get; set; }

        [JsonPropertyName("group")]
        public FirewallRuleDestinationGroup? Group { get; set; }
    }

    public class FirewallRuleDestinationGroup
    {
        [JsonPropertyName("port-group")]
        public string? PortGroup { get; set; }

        [JsonPropertyName("address-group")]
        public string? AddressGroup { get; set; }
    }
}
