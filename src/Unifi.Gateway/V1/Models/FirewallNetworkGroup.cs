using System.Text.Json.Serialization;

namespace Unifi.Gateway.V1.Models
{
    public class FirewallNetworkGroup
    {
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("network")]
        public List<string> Networks { get; set; } = [];
    }
}
