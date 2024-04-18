using System.Text.Json.Serialization;

namespace Unifi.Gateway.V1.Models
{
    public class UnifiNatSourceGroup
    {
        [JsonPropertyName("network-group")]
        public string NetworkGroup { get; set; } = string.Empty;
    }
}