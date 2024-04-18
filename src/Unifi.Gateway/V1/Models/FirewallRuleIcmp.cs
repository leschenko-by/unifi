using System.Text.Json.Serialization;

namespace Unifi.Gateway.V1.Models
{
    public class FirewallRuleIcmp
    {
        [JsonPropertyName("type-name")]
        public string? TypeName { get; set; }
    }
}
