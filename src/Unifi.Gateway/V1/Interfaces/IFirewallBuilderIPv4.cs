using Unifi.Gateway.V1.Models;

namespace Unifi.Gateway.V1.Interfaces
{
    public interface IFirewallBuilderIPv4
    {
        Task<string> BuildAsync(SystemConfiguration cfg);
    }
}
