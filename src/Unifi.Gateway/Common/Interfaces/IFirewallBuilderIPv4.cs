using Unifi.Gateway.Models.V1;

namespace Unifi.Gateway.Common.Interfaces
{
    public interface IFirewallBuilderIPv4
    {
        public string Build(SystemConfiguration cfg);
    }

    public interface IFirewallBuilderIPv6
    {
        public string Build(SystemConfiguration cfg);
    }
}
