using System.Text.Json.Serialization;

namespace Unifi.Gateway.Models.V1
{
    public class UnifiNatConfiguration
    {
        [JsonPropertyName("rule")]
        public Dictionary<string, UnifiNatRule> Rules { get; set; } = [];
    }
}
