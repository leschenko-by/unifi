using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Unifi.Gateway.Json
{
    public class Configuration
    {
        public string InformUrl { get; set; } = string.Empty;

        public string Key { get; set; } = string.Empty;

        public bool Adopted { get; set; }

        public string[] MgmtCfg { get; set; } = [];

        public string[] SystemCfg { get; set; } = [];

        public string Fingerprint { get; set; } = string.Empty;
    }

    public class ResponseData
    {
        [JsonPropertyName("_type")]
        public string Command { get; set; } = string.Empty;

        [JsonPropertyName("mgmt_cfg")]
        public string MgmtCfg { get; set; } = string.Empty;

        [JsonPropertyName("system_cfg")]
        public string SystemCfg { get; set; } = string.Empty;

        [JsonPropertyName("server_time_in_utc")]
        public string ServerTimeInUtc { get; set; } = string.Empty;
    }
}
