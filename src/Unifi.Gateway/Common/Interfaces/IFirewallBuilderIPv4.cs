using Unifi.Gateway.Models.V1;

namespace Unifi.Gateway.Common.Interfaces
{
    public interface IFirewallBuilderIPv4
    {
        Task<string> BuildAsync(SystemConfiguration cfg);
    }

    public interface IFirewallBuilderIPv6
    {
        Task<string> BuildAsync(SystemConfiguration cfg);
    }
}
