using System.Text.Json.Serialization;

namespace Unifi.Gateway.V1.Models
{
    public class FirewallConfiguration
    {
        [JsonPropertyName("name")]
        public Dictionary<string, FirewallRuleGroup> Names { get; set; } = [];

        [JsonPropertyName("ipv6-name")]
        public Dictionary<string, FirewallRuleGroup> NamesV6 { get; set; } = [];

        [JsonPropertyName("group")]
        public FirewallGroupConfiguration Groups { get; set; } = new();
    }

    public class FirewallGroupConfiguration
    {
        [JsonPropertyName("port-group")]
        public Dictionary<string, FirewallPortGroup> PortGroups { get; set; } = [];

        [JsonPropertyName("address-group")]
        public Dictionary<string, FirewallAddressGroup> AddressGroups { get; set; } = [];

        [JsonPropertyName("ipv6-address-group")]
        public Dictionary<string, FirewallAddressGroup> AddressGroupsV6 { get; set; } = [];

        [JsonPropertyName("network-group")]
        public Dictionary<string, FirewallNetworkGroup> NetworkGroups { get; set; } = [];

        [JsonPropertyName("ipv6-network-group")]
        public Dictionary<string, FirewallNetworkGroup> NetworkGroupsV6 { get; set; } = [];
    }
}
