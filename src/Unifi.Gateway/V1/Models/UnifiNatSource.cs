using System.Text.Json.Serialization;

namespace Unifi.Gateway.V1.Models
{
    public class UnifiNatSource
    {
        [JsonPropertyName("group")]
        public UnifiNatSourceGroup Group { get; set; } = new();
    }
}