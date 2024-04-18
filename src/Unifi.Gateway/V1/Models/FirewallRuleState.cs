using System.Text.Json.Serialization;

namespace Unifi.Gateway.V1.Models
{
    public class FirewallRuleState
    {
        [JsonPropertyName("established")]
        public string Established { get; set; } = string.Empty;

        [JsonPropertyName("invalid")]
        public string Invalid { get; set; } = string.Empty;

        [JsonPropertyName("new")]
        public string New { get; set; } = string.Empty;

        [JsonPropertyName("related")]
        public string Related { get; set; } = string.Empty;
    }
}