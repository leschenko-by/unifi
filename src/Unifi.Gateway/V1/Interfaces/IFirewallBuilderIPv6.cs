using Unifi.Gateway.V1.Models;

namespace Unifi.Gateway.V1.Interfaces
{
    public interface IFirewallBuilderIPv6
    {
        Task<string> BuildAsync(SystemConfiguration cfg);
    }
}
