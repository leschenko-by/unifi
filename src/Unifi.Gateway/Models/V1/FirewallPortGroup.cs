using System.Text.Json.Serialization;

namespace Unifi.Gateway.Models.V1
{
    public class FirewallPortGroup
    {
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("port")]
        public List<object> Ports { get; set; } = [];
    }
}
