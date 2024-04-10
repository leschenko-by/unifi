using System.Text.Json.Serialization;

namespace Unifi.Gateway.Models.V1
{
    public class FirewallRuleDestinationGroup
    {
        [JsonPropertyName("port-group")]
        public string? PortGroup { get; set; }

        [JsonPropertyName("address-group")]
        public string? AddressGroup { get; set; }

        [JsonPropertyName("ipv6-address-group")]
        public string? AddressGroupV6 { get; set; }
    }
}
