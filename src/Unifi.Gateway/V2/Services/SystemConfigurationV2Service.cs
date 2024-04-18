using Unifi.Gateway.Common.Interfaces;
using Unifi.Gateway.Models;

namespace Unifi.Gateway.V2.Services
{
    public class SystemConfigurationV2Service : ISystemConfigurationService
    {
        public async Task ApplyAsync(string systemCfg, Configuration configuration)
        {
            Directory.CreateDirectory("/etc/unifi");
            await File.WriteAllTextAsync("/etc/unifi/system.json", systemCfg);

            //var cfg = JsonSerializer.Deserialize(systemCfg, SourceGenerationContext.Default.SystemConfiguration);
            //if (cfg is null)
            //{
            //    return;
            //}

            //ApplyUnifiSettings(configuration, cfg);

            //await ApplyPortForwardingAsync(cfg);
        }

    }
}
