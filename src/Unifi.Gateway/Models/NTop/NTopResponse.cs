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
        public long Duration { get; set; }

        [JsonPropertyName("bytes.sent")]
        public long BytesSent { get; set; }

        [JsonPropertyName("bytes.rcvd")]
        public long BytesReceived { get; set; }

        [JsonPropertyName("packets.sent")]
        public long PacketsSent { get; set; }

        [JsonPropertyName("packets.rcvd")]
        public long PacketsReceived { get; set; }
    }
}
