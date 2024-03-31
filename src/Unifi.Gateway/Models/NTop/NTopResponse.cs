using System.Text.Json.Serialization;

namespace Unifi.Gateway.Models.NTop
{
    public class NTopResponse
    {
        [JsonPropertyName("rc_str")]
        public string ResponseCode { get; set; } = string.Empty;

        [JsonPropertyName("rsp")]
        public NTopHost[] Hosts { get; set; } = [];
    }

    public class NTopHost
    {
        [JsonPropertyName("mac")]
        public string? MacAddress { get; set; }

        [JsonPropertyName("router")]
        public string? Router { get; set; }

        [JsonPropertyName("ip")]
        public string? IpAddress { get; set; }

        [JsonPropertyName("ip_version")]
        public int IpVersion { get; set; }

        [JsonPropertyName("duration")]
        public int Duration { get; set; }

        [JsonPropertyName("bytes.sent")]
        public int BytesSent { get; set; }

        [JsonPropertyName("bytes.rcvd")]
        public int BytesReceived { get; set; }

        [JsonPropertyName("packets.sent")]
        public int PacketsSent { get; set; }

        [JsonPropertyName("packets.rcvd")]
        public int PacketsReceived { get; set; }
    }
}
