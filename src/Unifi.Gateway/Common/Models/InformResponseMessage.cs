using System.Text.Json.Serialization;

namespace Unifi.Gateway.Common.Models
{
    public class InformResponseMessage
    {
        [JsonPropertyName("_type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("mgmt_cfg")]
        public string MgmtCfg { get; set; } = string.Empty;

        [JsonPropertyName("system_cfg")]
        public string SystemCfg { get; set; } = string.Empty;

        [JsonPropertyName("server_time_in_utc")]
        public string ServerTimeInUtc { get; set; } = string.Empty;

        [JsonPropertyName("interval")]
        public int? Interval { get; set; }

        [JsonPropertyName("immediate")]
        public int? Immediate { get; set; }

        [JsonPropertyName("version")]
        public string? Firmware { get; set; }

        [JsonPropertyName("cmd")]
        public string? Command { get; set; }
    }
}
