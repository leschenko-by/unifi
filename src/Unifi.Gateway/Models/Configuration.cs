namespace Unifi.Gateway.Models
{
    public class Configuration
    {
        public string InformUrl { get; set; } = string.Empty;

        public string Key { get; set; } = string.Empty;

        public bool Adopted { get; set; }

        public string[] MgmtCfg { get; set; } = [];

        public string Fingerprint { get; set; } = string.Empty;

        public string Firmware { get; set; } = string.Empty;
    }
}
