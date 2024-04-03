using System.Text;
using System.Text.Json;
using Unifi.Gateway.Common.Interfaces;
using Unifi.Gateway.Models;
using Unifi.Gateway.Models.V1;

namespace Unifi.Gateway.Common.Services
{
    public class SystemConfigurationV1Service(IUfwService ufw) : ISystemConfigurationService
    {
        public readonly IUfwService ufw = ufw;

        public async Task ApplyAsync(string systemCfg, Configuration configuration)
        {
            Directory.CreateDirectory("/etc/unifi");
            await File.WriteAllTextAsync("/etc/unifi/system.json", systemCfg);

            var cfg = JsonSerializer.Deserialize(systemCfg, SourceGenerationContext.Default.SystemConfigurationV1);
            if (cfg is null)
            {
                return;
            }

            ApplyUnifiSettings(configuration, cfg);

            await ApplyPortForwardingAsync(cfg);
        }

        private static void ApplyUnifiSettings(Configuration configuration, SystemConfiguration cfg)
        {
            configuration.EchoServer = cfg.Unifi.EchoServer;
            configuration.ConfigNetworkWAN = cfg.Unifi.ConfigNetworkWAN;
            configuration.ConfigNetworkWAN2 = cfg.Unifi.ConfigNetworkWAN2;
        }

        private async Task ApplyPortForwardingAsync(SystemConfiguration cfg)
        {
            var lines = await File.ReadAllLinesAsync("/etc/ufw/before.rules");

            var (output, changed) = BuildFirewallRules(cfg, lines);

            if (changed)
            {
                await File.WriteAllTextAsync("/etc/ufw/before.rules", output);
                await ufw.ReloadAsync();
            }
        }

        private static (string, bool) BuildFirewallRules(SystemConfiguration cfg, string[] lines)
        {
            var started = false;
            var finished = false;
            var injected = false;
            var builder = new StringBuilder();
            foreach (var line in lines)
            {
                if (started && !finished && !injected)
                {
                    BuildFirewallRules(cfg, builder);
                    injected = true;
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
                    builder.AppendLine(line);
                }
            }

            var input = string.Join("\r\n", lines.Concat([""])).ReplaceLineEndings();
            var output = builder.ToString().ReplaceLineEndings();

            return (output, input != output);
        }

        private static void BuildFirewallRules(SystemConfiguration cfg, StringBuilder output)
        {
            var cache = new HashSet<string>();

            foreach (var (_, rule) in cfg.Unifi.PortForward.Rules)
            {
                var data = rule.Split(",").Select(t => t.Split("=")).ToDictionary(t => t[0], t => t[1]);
                var source = data["src"];
                var originalPort = data["dst_port"].Trim('\'');
                var address = data["fwd"];
                var targetPort = data["fwd_port"].Trim('\'');
                var tcp = data["tcp"] == "1";
                var udp = data["udp"] == "1";
                if (!tcp && !udp) continue;

                var protocol = (tcp, udp) switch
                {
                    (true, false) => "tcp",
                    (false, true) => "udp",
                    _ => "tcp_udp"
                };

                if (!CheckPortForwardRules(cfg.PortForward.Rules.Select(t => t.Value))) continue;
                if (!CheckFirewallRules(cfg.Firewall.Groups["WAN_IN"].Rules.Select(t => t.Value))) continue;

                var sourceRule = source == "0.0.0.0" ? "-i eth0" : "-s " + source;

                if (tcp)
                {
                    output.AppendLine($"-A PREROUTING {sourceRule} -p tcp --dport {originalPort} -j DNAT --to-destination {address}:{targetPort}");
                }
                if (udp)
                {
                    output.AppendLine($"-A PREROUTING {sourceRule} -p udp --dport {originalPort} -j DNAT --to-destination {address}:{targetPort}");
                }

                bool CheckFirewallRules(IEnumerable<FirewallRule> rules)
                {
                    foreach (var rule in rules)
                    {
                        if (rule.Action != "accept") continue;
                        if (rule.Protocol != protocol) continue;
                        if (rule.Destination?.Address != address) continue;
                        if (rule.Destination?.Port != targetPort) continue;

                        if (source == "0.0.0.0" && rule.Source != null) continue;
                        if (source != "0.0.0.0" && rule.Source?.Address != source) continue;

                        return true;
                    }

                    return false;
                }

                bool CheckPortForwardRules(IEnumerable<PortForwardRule> rules)
                {
                    foreach (var rule in rules)
                    {
                        if (rule.Protocol != protocol) continue;
                        if (rule.OriginalPort != originalPort) continue;
                        if (rule.Destination.Address != address) continue;
                        var destinationPort = rule.Destination.Port ?? rule.OriginalPort;
                        if (destinationPort != targetPort) continue;

                        return true;
                    }

                    return false;
                }
            }
        }
    }
}
