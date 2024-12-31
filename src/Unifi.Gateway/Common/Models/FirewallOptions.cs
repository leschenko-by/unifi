namespace Unifi.Gateway.Common.Models
{
    public class FirewallOptions
    {
        public string IPv4WANs { get; set; } = "eth0";
        public string IPv4LANs { get; set; } = "eth1";
        public string IPv6WANs { get; set; } = "eth0";
        public string IPv6LANs { get; set; } = "eth1";
    }
}
