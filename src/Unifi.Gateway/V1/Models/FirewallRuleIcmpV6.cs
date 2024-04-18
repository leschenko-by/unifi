using System.Text.Json.Serialization;

namespace Unifi.Gateway.V1.Models
{
    public class FirewallRuleIcmpV6
    {
        [JsonPropertyName("type")]
        public string? TypeName { get; set; }
    }
}
