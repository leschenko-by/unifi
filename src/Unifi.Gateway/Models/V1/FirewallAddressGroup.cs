using System.Text.Json.Serialization;

namespace Unifi.Gateway.Models.V1
{
    public class FirewallAddressGroup
    {
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("address")]
        public List<string> Addresses { get; set; } = [];

        [JsonPropertyName("ipv6-address")]
        public List<string> AddressesV6 { get; set; } = [];
    }
}
