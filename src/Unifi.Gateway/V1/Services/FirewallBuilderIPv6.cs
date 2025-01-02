using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json.Nodes;
using Unifi.Gateway.Common.Interfaces;
using Unifi.Gateway.Common.Models;
using Unifi.Gateway.V1.Interfaces;
using Unifi.Gateway.V1.Models;

namespace Unifi.Gateway.V1.Services
{
    public class FirewallBuilderIPv6(IFileReader fileReader, IOptions<FirewallOptions> options) : IFirewallBuilderIPv6
    {
        private readonly IFileReader fileReader = fileReader;
        private readonly IOptions<FirewallOptions> options = options;

        public async Task<string> BuildAsync(SystemConfiguration cfg)
        {
            var wans = options.Value.IPv6WANs.Split(",");
            var lans = options.Value.IPv6LANs.Split(",");
            var rules = new StringBuilder();

            await AppendFilterRulesAsync(cfg, rules, wans, lans);

            return rules.ToString();
        }

        private async Task AppendFilterRulesAsync(SystemConfiguration cfg, StringBuilder rules, string[] wans, string[] lans)
        {
            rules.AppendLine($"""
                # WANs: '{string.Join(", ", wans)}'
                # LANs: '{string.Join(", ", lans)}'
                
                *filter
                :INPUT DROP [0:0]
                :FORWARD DROP [0:0]
                :OUTPUT DROP [0:0]

                :unifi6-before-input - [0:0]
                :unifi6-user-input - [0:0]
                :unifi6-after-input - [0:0]

                :unifi6-before-forward - [0:0]
                :unifi6-user-forward - [0:0]
                :unifi6-after-forward - [0:0]

                :unifi6-before-output - [0:0]
                :unifi6-user-output - [0:0]
                :unifi6-after-output - [0:0]

                :unifi6-log-accept - [0:0]
                :unifi6-log-reject - [0:0]
                :unifi6-log-drop - [0:0]
                
                -A INPUT -j unifi6-before-input
                -A INPUT -j unifi6-user-input
                -A INPUT -j unifi6-after-input
                -A FORWARD -j unifi6-before-forward
                -A FORWARD -j unifi6-user-forward
                -A FORWARD -j unifi6-after-forward
                -A OUTPUT -j unifi6-before-output
                -A OUTPUT -j unifi6-user-output
                -A OUTPUT -j unifi6-after-output

                -A unifi6-log-accept -j LOG --log-prefix="[unifi accept] "
                -A unifi6-log-accept -j ACCEPT
                
                -A unifi6-log-reject -j LOG --log-prefix="[unifi reject] "
                -A unifi6-log-reject -j REJECT
                
                -A unifi6-log-drop -j LOG --log-prefix="[unifi drop] "
                -A unifi6-log-drop -j DROP
                
                -A unifi6-before-input -i lo -j ACCEPT
                -A unifi6-before-output -o lo -j ACCEPT
                -A unifi6-before-input -m rt --rt-type 0 -j DROP
                -A unifi6-before-output -m rt --rt-type 0 -j DROP
                """);

            foreach (var lan in lans)
            {
                rules.AppendLine($"""
                    -A unifi6-before-input -i {lan} -m conntrack --ctstate RELATED,ESTABLISHED -j ACCEPT
                    -A unifi6-before-input -i {lan} -m conntrack --ctstate INVALID -j DROP
                    -A unifi6-after-output -o {lan} -j ACCEPT
                    """);
                rules.AppendLine();
            }

            var custom = await fileReader.ReadAsync("/etc/iptables/custom-filter.v6");
            if (!string.IsNullOrEmpty(custom))
            {
                rules.AppendLine("# Custom");
                rules.AppendLine(custom);
                rules.AppendLine();
            }

            foreach (var wan in wans)
            {
                AppendRules(rules, BuildFilters(cfg, "WANv6_LOCAL", wan, "unifi6-user-input", true));
                AppendRules(rules, BuildFilters(cfg, "WANv6_IN", wan, "unifi6-user-forward", true));
                AppendRules(rules, BuildFilters(cfg, "WANv6_OUT", wan, "unifi6-user-output", false));
            }
            foreach (var lan in lans)
            {
                AppendRules(rules, BuildFilters(cfg, "LANv6_LOCAL", lan, "unifi6-user-input", true));
                AppendRules(rules, BuildFilters(cfg, "LANv6_IN", lan, "unifi6-user-forward", true));
                AppendRules(rules, BuildFilters(cfg, "LANv6_OUT", lan, "unifi6-user-forward", false));
            }

            rules.AppendLine("COMMIT");
            rules.AppendLine();
        }

        private static IEnumerable<string> BuildFilters(SystemConfiguration cfg, string name, string nic, string chain, bool input)
        {
            var direction = input ? "-i" : "-o";
            yield return $"# {name}";
            foreach (var rule in cfg.Firewall.NamesV6[name].Rules.OrderBy(t => Convert.ToInt32(t.Key)).Select(t => t.Value))
            {
                var action = rule.Action.ToUpper();
                if (rule.Log == "enable")
                {
                    action = "unifi6-log-" + action.ToLower();
                }

                var state = GetState(rule);

                if (string.IsNullOrEmpty(state))
                {
                    state = GetPolicy(input, rule);
                }

                var protos = GetProtocols(rule);

                var dports = GetPorts(cfg, rule.Destination) switch
                {
                    var t when t.Length > 0 => t.Select(p => "--dport " + p).ToArray(),
                    _ => [""]
                };

                var sports = GetPorts(cfg, rule.Source) switch
                {
                    var t when t.Length > 0 => t.Select(p => "--sport " + p).ToArray(),
                    _ => [""]
                };

                var destinations = GetAddresses(cfg, rule.Destination) switch
                {
                    var t when t.Length > 0 => t.Select(d => "-d " + d).ToArray(),
                    _ => [""]
                };

                var sources = GetAddresses(cfg, rule.Source) switch
                {
                    var t when t.Length > 0 => t.Select(d => "-s " + d).ToArray(),
                    _ => [""]
                };

                var sourceMac = rule.Source?.MacAddress ?? string.Empty;
                if (!string.IsNullOrEmpty(sourceMac))
                {
                    sourceMac = "--mac-source " + sourceMac;
                }

                foreach (var source in sources)
                {
                    foreach (var sport in sports)
                    {
                        foreach (var destination in destinations)
                        {
                            foreach (var dport in dports)
                            {
                                foreach (var proto in protos)
                                {
                                    yield return $"-A {chain} {direction} {nic} {source} {sourceMac} {destination} {proto} {sport} {dport} {state} -j {action} #{rule.Description}";
                                }
                            }
                        }
                    }
                }
            }

            var defaultAction = cfg.Firewall.NamesV6[name].DefaultAction;
            if (defaultAction != "drop")
            {
                yield return $"-A {chain} {direction} {nic} -j {defaultAction.ToUpper()}";
            }
            yield return string.Empty;
        }

        private static string GetPolicy(bool input, FirewallRule rule)
        {
            if (rule.Ipsec != null)
            {
                string pol;
                if (rule.Ipsec.ToString() == "match-ipsec")
                {
                    pol = "ipsec";
                }
                else if (rule.Ipsec is JsonObject obj)
                {
                    pol = obj["match-ipsec"] != null ? "ipsec" : "none";
                }
                else
                {
                    return "";
                }

                var policyDirection = input ? "in" : "out";

                return $"-m policy --pol {pol} --dir {policyDirection}";
            }
            return string.Empty;
        }

        private static string GetState(FirewallRule rule)
        {
            var conntrack = "";
            if (rule.State is not null)
            {
                var states = new List<string>();
                if (rule.State.Established == "enable")
                {
                    states.Add("ESTABLISHED");
                }
                if (rule.State.Related == "enable")
                {
                    states.Add("RELATED");
                }
                if (rule.State.Invalid == "enable")
                {
                    states.Add("INVALID");
                }
                if (rule.State.New == "enable")
                {
                    states.Add("NEW");
                }
                conntrack = $"-m conntrack --ctstate {string.Join(",", states)}";
            }

            return conntrack;
        }

        private static void AppendRules(StringBuilder rules, IEnumerable<string> lines)
        {
            foreach (var line in lines)
            {
                var rule = line;
                int i;
                while ((i = rule.IndexOf("  ")) >= 0)
                {
                    rule = rule.Remove(i, 1);
                }
                rules.AppendLine(rule);
            }
        }

        private static List<string> GetProtocols(FirewallRule rule)
        {
            var protocol = rule.Protocol;
            if (string.IsNullOrEmpty(protocol))
            {
                return [""];
            }

            var opposite = protocol.StartsWith('!');
            var icmpType = rule.Icmpv6?.TypeName;
            var result = GetProtocols(protocol.TrimStart('!'), icmpType);
            return opposite ? [.. result.Select(rule => "! " + rule)] : result;

            static List<string> GetProtocols(string protocol, string? icmpType)
            {
                if (protocol == "all")
                {
                    return [""];
                }
                else if (protocol == "tcp_udp")
                {
                    return ["-p tcp", "-p udp"];
                }
                else if (protocol == "tcp" || protocol == "udp")
                {
                    return ["-p " + protocol];
                }
                else if (protocol == "ipv6-icmp" || protocol == "icmpv6")
                {
                    var proto = "-p ipv6-icmp";
                    if (!string.IsNullOrEmpty(icmpType) && icmpType != "any")
                    {
                        proto += " --icmpv6-type " + icmpType;
                    }
                    return [proto];
                }
                return [];
            }
        }

        private static int[] GetPorts(SystemConfiguration cfg, FirewallRuleDestination? destination)
        {
            var name = destination?.Group?.PortGroup;
            if (!string.IsNullOrEmpty(name))
            {
                return cfg.Firewall.Groups.PortGroups[name].Ports.Select(t => Convert.ToInt32(t.ToString())).ToArray();
            }
            if (!string.IsNullOrEmpty(destination?.Port))
            {
                return [Convert.ToInt32(destination?.Port)];
            }
            return [];
        }

        private static string[] GetAddresses(SystemConfiguration cfg, FirewallRuleDestination? destination)
        {
            var name = destination?.Group?.AddressGroupV6;
            if (!string.IsNullOrEmpty(name))
            {
                if (cfg.Firewall.Groups.AddressGroupsV6.TryGetValue(name, out var group))
                {
                    return [.. group.AddressesV6];
                }
            }
            else if (!string.IsNullOrEmpty(destination?.Address))
            {
                return [destination.Address];
            }
            return [];
        }
    }
}
