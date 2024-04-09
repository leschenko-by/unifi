namespace Unifi.Gateway.Common.Interfaces
{
    public interface IFirewallService
    {
        Task ApplyIPv4RulesAsync(string rules);
        Task ApplyIPv6RulesAsync(string rules);
    }
}
