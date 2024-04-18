using System.Text.Json.Serialization;

namespace Unifi.Gateway.V1.Models
{
    public class PortForwardConfiguration
    {
        [JsonPropertyName("rule")]
        public Dictionary<string, PortForwardRule> Rules { get; set; } = new();
    }
}
