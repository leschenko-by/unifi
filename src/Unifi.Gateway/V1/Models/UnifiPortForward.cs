using System.Text.Json.Serialization;

namespace Unifi.Gateway.V1.Models
{
    public class UnifiPortForward
    {
        [JsonPropertyName("pfor.rules")]
        public Dictionary<string, string> Rules { get; set; } = [];
    }
}
