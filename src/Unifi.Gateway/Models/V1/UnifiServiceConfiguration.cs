using System.Text.Json.Serialization;

namespace Unifi.Gateway.Models.V1
{
    public class UnifiServiceConfiguration
    {
        [JsonPropertyName("nat")]
        public UnifiNatConfiguration Nat { get; set; } = new();
    }
}
