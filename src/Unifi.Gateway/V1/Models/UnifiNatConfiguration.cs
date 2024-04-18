using System.Text.Json.Serialization;

namespace Unifi.Gateway.V1.Models
{
    public class UnifiNatConfiguration
    {
        [JsonPropertyName("rule")]
        public Dictionary<string, UnifiNatRule> Rules { get; set; } = [];
    }
}
