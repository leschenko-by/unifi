using System.Text.Json.Serialization;

namespace Unifi.Gateway.Models.V1
{
    public class PortForwardConfiguration
    {
        [JsonPropertyName("rule")]
        public Dictionary<string, PortForwardRule> Rules { get; set; } = new();
    }
}
