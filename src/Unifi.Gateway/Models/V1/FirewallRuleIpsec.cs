using System.Text.Json.Serialization;

namespace Unifi.Gateway.Models.V1
{
    public class FirewallRuleIpsec
    {
        [JsonPropertyName("match-ipsec")]
        public string? MatchIpSec { get; set; }

        [JsonPropertyName("match-none")]
        public string? MatchNone { get; set; }
    }
}
