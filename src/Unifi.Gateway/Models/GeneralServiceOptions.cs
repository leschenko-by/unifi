namespace Unifi.Gateway.Models
{
    public class GeneralServiceOptions
    {
        public string Firmware { get; set; } = "4.4.44.5213871";
        public string Ports { get; set; } = "eth0,eth1";
        public int DiscoveryPortId { get; set; } = 1;
    }
}