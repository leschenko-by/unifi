using System.Text.Json.Serialization;

namespace Unifi.Gateway.V1.Models
{
    public class UnifiServiceConfiguration
    {
        [JsonPropertyName("nat")]
        public UnifiNatConfiguration Nat { get; set; } = new();
    }
}
