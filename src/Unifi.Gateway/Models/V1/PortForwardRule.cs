using System.Text.Json.Serialization;

namespace Unifi.Gateway.Models.V1
{
    public class PortForwardRule
    {
        [JsonPropertyName("forward-to")]
        public ForwardDestination Destination { get; set; } = new();

        [JsonPropertyName("original-port")]
        public string OriginalPort { get; set; } = string.Empty;

        [JsonPropertyName("protocol")]
        public string Protocol { get; set; } = string.Empty;
    }
}
