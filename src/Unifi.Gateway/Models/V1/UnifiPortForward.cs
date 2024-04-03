using System.Text.Json.Serialization;

namespace Unifi.Gateway.Models.V1
{
    public class UnifiPortForward
    {
        [JsonPropertyName("pfor.rules")]
        public Dictionary<string, string> Rules { get; set; } = [];
    }
}
