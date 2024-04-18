using System.Text.Json.Serialization;

namespace Unifi.Gateway.V1.Models
{
    public class ForwardDestination
    {
        [JsonPropertyName("address")]
        public string Address { get; set; } = string.Empty;

        [JsonPropertyName("port")]
        public string? Port { get; set; }
    }
}
