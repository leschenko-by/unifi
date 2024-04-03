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
            if (cfg is not null)
            {
                configuration.EchoServer = cfg.Unifi.EchoServer;
                configuration.ConfigNetworkWAN = cfg.Unifi.ConfigNetworkWAN;
                configuration.ConfigNetworkWAN2 = cfg.Unifi.ConfigNetworkWAN2;

                await ApplyFirewallSettingsAsync(cfg);
            }
        }

        private async Task ApplyFirewallSettingsAsync(SystemConfiguration cfg)
        {
            var nats = GetNatTables(cfg).ToArray();
            var mangles = GetMangleTables(cfg).ToArray();
            var filters = GetFilterTables(cfg).ToArray();

            var lines = await File.ReadAllLinesAsync("/etc/ufw/before.rules");

            var (output, changed) = ApplySettings(lines, nats, mangles, filters);

            if (changed)
            {
                await File.WriteAllTextAsync("/etc/ufw/before.rules", output);
                await ufw.ReloadAsync();
            }
        }

        private static (string, bool) ApplySettings(string[] lines, string[] nats, string[] mangles, string[] filters)
        {
            var rules = lines.ToList();
            Inject("nat", rules, nats);
            Inject("mangle", rules, mangles);
            Inject("filter", rules, filters);

            var input = string.Join("\r\n", lines.Concat([""])).ReplaceLineEndings();
            var output = string.Join("\r\n", rules.Concat([""])).ReplaceLineEndings();

            return (output, input != output);

            static void Inject(string key, List<string> lines, string[] rules)
            {
                var start = lines.IndexOf($"# unifi-{key}-start");
                var end = lines.IndexOf($"# unifi-{key}-end");

                if (start != -1 && end != -1 && end > start)
                {
                    lines.RemoveRange(start + 1, end - start - 1);
                    lines.InsertRange(start + 1, rules);
                }
            }
        }

        private static IEnumerable<string> GetNatTables(SystemConfiguration cfg)
        {
            var cache = new HashSet<string>();

            foreach (var (_, rule) in cfg.Unifi.PortForward.Rules)
            {
                yield return "# " + rule;

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
                if (!CheckFirewallRules(cfg.Firewall.Names["WAN_IN"].Rules.Select(t => t.Value))) continue;

                var sourceRule = source == "0.0.0.0" ? "-i eth0" : "-s " + source;

                if (tcp)
                {
                    yield return $"-A PREROUTING {sourceRule} -p tcp --dport {originalPort} -j DNAT --to-destination {address}:{targetPort}";
                }
                if (udp)
                {
                    yield return $"-A PREROUTING {sourceRule} -p udp --dport {originalPort} -j DNAT --to-destination {address}:{targetPort}";
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

        private static IEnumerable<string> GetMangleTables(SystemConfiguration cfg)
        {
            yield break;
        }

        private static IEnumerable<string> GetFilterTables(SystemConfiguration cfg)
        {
            var nics = new List<(string, string)>
            {
                ("WAN_LOCAL", "eth0"),
                ("LAN_IN", "eth1"),
            };

            foreach (var (name, nic) in nics)
            {
                foreach (var rule in cfg.Firewall.Names[name].Rules.Select(t => t.Value))
                {
                    if (string.IsNullOrEmpty(rule.Protocol)) continue;
                    if (!string.IsNullOrEmpty(rule.Destination?.Address)) continue;
                    if (!string.IsNullOrEmpty(rule.Destination?.Port)) continue;

                    var group = rule.Destination?.Group;

                    var protos = GetProtos(rule);

                    var destPorts = GetPorts(group);
                    var port = destPorts.Length switch
                    {
                        0 => "",
                        1 => "--dport " + destPorts.First(),
                        _ => "-m multiport --dports " + string.Join(",", destPorts)
                    };

                    var destAddrs = GetAddresses(group);
                    var destinations = destAddrs.Length > 0
                        ? destAddrs.Select(d => "-d " + d).ToArray()
                        : [""];

                    foreach (var destination in destinations)
                    {
                        foreach (var proto in protos)
                        {
                            yield return $"-A INPUT -i {nic} {destination} -p {proto} {port} -j {rule.Action.ToUpper()}".Replace("  ", " ");
                        }
                    }
                }
            }

            string[] GetAddresses(FirewallRuleDestinationGroup? rule)
            {
                var name = rule?.AddressGroup;
                if (!string.IsNullOrEmpty(name))
                {
                    return cfg.Firewall.Groups.AddressGroups[name].Addresses.ToArray();
                }
                return [];
            }

            int[] GetPorts(FirewallRuleDestinationGroup? rule)
            {
                var name = rule?.PortGroup;
                if (!string.IsNullOrEmpty(name))
                {
                    return cfg.Firewall.Groups.PortGroups[name].Ports.Select(t => Convert.ToInt32(t.ToString())).ToArray();
                }
                return [];
            }

            static List<string> GetProtos(FirewallRule rule)
            {
                var protos = new List<string>();
                if (rule.Protocol == "tcp_udp")
                {
                    protos.Add("tcp");
                    protos.Add("udp");
                }
                else if (!string.IsNullOrEmpty(rule.Protocol))
                {
                    protos.Add(rule.Protocol);
                }

                return protos;
            }
        }
    }
}
