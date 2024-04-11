using System.Text.Json.Serialization;

namespace Unifi.Gateway.Models.V1
{
    public class UnifiNatSource
    {
        [JsonPropertyName("group")]
        public UnifiNatSourceGroup Group { get; set; } = new();
    }
}