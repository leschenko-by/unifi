using System.Text.Json.Serialization;

namespace Unifi.Gateway.Models.V1
{
    public class FirewallRuleIcmp
    {
        [JsonPropertyName("type-name")]
        public string? TypeName { get; set; }
    }
}
