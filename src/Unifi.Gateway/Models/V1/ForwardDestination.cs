using System.Text.Json.Serialization;

namespace Unifi.Gateway.Models.V1
{
    public class ForwardDestination
    {
        [JsonPropertyName("address")]
        public string Address { get; set; } = string.Empty;

        [JsonPropertyName("port")]
        public string? Port { get; set; }
    }
}
