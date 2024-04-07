using System.Text.Json.Serialization;

namespace Unifi.Gateway.Models.V1
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
    }

    public class FirewallPortGroup
    {
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("port")]
        public List<object> Ports { get; set; } = [];
    }

    public class FirewallAddressGroup
    {
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("address")]
        public List<string> Addresses { get; set; } = [];
    }
}
