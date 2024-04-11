using System.Text.Json.Serialization;

namespace Unifi.Gateway.Models.V1
{
    public class UnifiNatSourceGroup
    {
        [JsonPropertyName("network-group")]
        public string NetworkGroup { get; set; } = string.Empty;
    }
}