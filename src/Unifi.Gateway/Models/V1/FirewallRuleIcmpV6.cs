using System.Text.Json.Serialization;

namespace Unifi.Gateway.Models.V1
{
    public class FirewallRuleIcmpV6
    {
        [JsonPropertyName("type")]
        public string? TypeName { get; set; }
    }
}
