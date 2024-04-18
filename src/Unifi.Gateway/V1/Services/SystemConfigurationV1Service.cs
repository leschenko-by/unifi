using System.Text.Json;
using Unifi.Gateway.Common.Interfaces;
using Unifi.Gateway.Common.Models;
using Unifi.Gateway.V1.Interfaces;

namespace Unifi.Gateway.V1.Services
{
    public class SystemConfigurationV1Service(
        IFirewallBuilderIPv4 ipv4builder,
        IFirewallBuilderIPv6 ipv6builder,
        IFirewallService firewallService,
        ILogger<SystemConfigurationV1Service> logger) : ISystemConfigurationService
    {
        private readonly IFirewallBuilderIPv4 ipv4Builder = ipv4builder;
        private readonly IFirewallBuilderIPv6 ipv6Builder = ipv6builder;
        private readonly IFirewallService firewallService = firewallService;
        private readonly ILogger<SystemConfigurationV1Service> logger = logger;

        public async Task ApplyAsync(string systemCfg, Configuration configuration)
        {
            Directory.CreateDirectory("/etc/unifi");
            await File.WriteAllTextAsync("/etc/unifi/system.json", systemCfg);

            var cfg = JsonSerializer.Deserialize(systemCfg, SourceGenerationContext.Default.SystemConfigurationV1);
            if (cfg is not null)
            {
                configuration.EchoServer = cfg.Unifi.EchoServer;
                configuration.ConfigNetworkWAN = cfg.Unifi.ConfigNetworkWAN;
                configuration.ConfigNetworkWAN2 = cfg.Unifi.ConfigNetworkWAN2;

                try
                {
                    await firewallService.ApplyIPv4RulesAsync(await ipv4Builder.BuildAsync(cfg));
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Can't apply IPv4 firewall rules.");
                }

                try
                {
                    await firewallService.ApplyIPv6RulesAsync(await ipv6Builder.BuildAsync(cfg));
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Can't apply IPv6 firewall rules.");
                }
            }
        }
    }
}
