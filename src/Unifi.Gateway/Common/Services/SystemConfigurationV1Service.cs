using System.Text;
using System.Text.Json;
using Unifi.Gateway.Common.Interfaces;
using Unifi.Gateway.Json;
using Unifi.Gateway.Models;

namespace Unifi.Gateway.Common.Services
{
    public class SystemConfigurationV1Service : ISystemConfigurationService
    {
        public readonly IUfwService ufw;

        public SystemConfigurationV1Service(IUfwService ufw)
        {
            this.ufw = ufw;
        }

        public async Task ApplyAsync(string systemCfg, Configuration configuration)
        {
            Directory.CreateDirectory("/etc/unifi");
            await File.WriteAllTextAsync("/etc/unifi/system.json", systemCfg);

            var cfg = JsonSerializer.Deserialize(systemCfg, SourceGenerationContext.Default.SystemConfiguration);
            if (cfg is null)
            {
                return;
            }

            ApplyUnifiSettings(configuration, cfg);

            await ApplyPortForwardingAsync(cfg);
        }

        private void ApplyUnifiSettings(Configuration configuration, SystemConfiguration cfg)
        {
            configuration.EchoServer = cfg.Unifi.EchoServer;
            configuration.ConfigNetworkWAN = cfg.Unifi.ConfigNetworkWAN;
            configuration.ConfigNetworkWAN2 = cfg.Unifi.ConfigNetworkWAN2;
        }

        private async Task ApplyPortForwardingAsync(SystemConfiguration cfg)
        {
            await Task.Yield();

            var started = false;
            var finished = false;
            var injected = false;
            var output = new StringBuilder();
            var lines = File.ReadAllLines("/etc/ufw/before.rules");
            foreach (var line in lines)
            {
                if (started && !finished)
                {
                    if (!injected)
                    {
                        foreach (var (_, rule) in cfg.PortForward.Rules)
                        {
                            var originalPort = rule.OriginalPort;
                            var targetPort = rule.Destination.Port ?? rule.OriginalPort;
                            var address = rule.Destination.Address;
                            if (rule.Protocol.Contains("tcp"))
                            {
                                output.AppendLine($"-A PREROUTING -i eth0 -p tcp --dport {originalPort} -j DNAT --to-destination {address}:{targetPort}");
                            }
                            if (rule.Protocol.Contains("udp"))
                            {
                                output.AppendLine($"-A PREROUTING -i eth0 -p udp --dport {originalPort} -j DNAT --to-destination {address}:{targetPort}");
                            }
                        }

                        injected = true;
                    }
                }

                if (line.StartsWith("# unifi start marker"))
                {
                    started = true;
                }
                if (line.StartsWith("# unifi end marker"))
                {
                    finished = true;
                }

                if (!started || finished || line.StartsWith("# unifi"))
                {
                    output.AppendLine(line);
                }
            }

            var input = string.Join("\r\n", lines.Concat([""])).ReplaceLineEndings();
            var finalConfig = output.ToString().ReplaceLineEndings();

            if (input != finalConfig)
            {
                await File.WriteAllTextAsync("/etc/ufw/before.rules", finalConfig);
                await ufw.ReloadAsync();
            }
        }
    }
}
