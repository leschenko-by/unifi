using System.Text;
using Unifi.Gateway.Common.Interfaces;
using Unifi.Gateway.Models.V1;

namespace Unifi.Gateway.Common.Services
{
    public class FirewallBuilderIPv6 : IFirewallBuilderIPv6
    {
        public string Build(SystemConfiguration cfg)
        {
            const string wan = "eth0";
            const string lan = "eth1";
            var rules = new StringBuilder();

            AppendFilterRules(cfg, rules, wan, lan);

            return rules.ToString();
        }

        private static void AppendFilterRules(SystemConfiguration cfg, StringBuilder rules, string wan, string lan)
        {
            rules.AppendLine($"""
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

                -A INPUT -j unifi6-before-input
                -A INPUT -j unifi6-user-input
                -A INPUT -j unifi6-after-input
                -A FORWARD -j unifi6-before-forward
                -A FORWARD -j unifi6-user-forward
                -A FORWARD -j unifi6-after-forward
                -A OUTPUT -j unifi6-before-output
                -A OUTPUT -j unifi6-user-output
                -A OUTPUT -j unifi6-after-output

                -A unifi6-before-input -i lo -j ACCEPT
                -A unifi6-before-output -o lo -j ACCEPT
                -A unifi6-before-input -m rt --rt-type 0 -j DROP
                -A unifi6-before-output -m rt --rt-type 0 -j DROP
                -A unifi6-before-input -i {lan} -m conntrack --ctstate RELATED,ESTABLISHED -j ACCEPT
                -A unifi6-before-input -i {lan} -m conntrack --ctstate INVALID -j DROP
                -A unifi6-after-output -o {lan} -j ACCEPT

                """);

            AppendRules(rules, BuildFilters(cfg, "WANv6_LOCAL", wan, "unifi6-user-input", true));
            AppendRules(rules, BuildFilters(cfg, "WANv6_IN", wan, "unifi6-user-forward", true));
            AppendRules(rules, BuildFilters(cfg, "WANv6_OUT", wan, "unifi6-user-output", false));
            AppendRules(rules, BuildFilters(cfg, "LANv6_LOCAL", lan, "unifi6-user-input", true));
            AppendRules(rules, BuildFilters(cfg, "LANv6_IN", lan, "unifi6-user-forward", true));
            AppendRules(rules, BuildFilters(cfg, "LANv6_OUT", lan, "unifi6-user-forward", false));

            rules.AppendLine("COMMIT");
            rules.AppendLine();
        }

        private static IEnumerable<string> BuildFilters(SystemConfiguration cfg, string name, string nic, string chain, bool input)
        {
            var direction = input ? "-i" : "-o";
            yield return $"# {name}";
            foreach (var rule in cfg.Firewall.NamesV6[name].Rules.Select(t => t.Value))
            {
                var action = rule.Action.ToUpper();
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
                    yield return $"-A {chain} {direction} {nic} -m conntrack --ctstate {string.Join(",", states)} -j {action}";
                    continue;
                }

                var protos = GetProtos(rule);

                var destPorts = GetPorts(cfg, rule.Destination);
                var dport = destPorts.Length switch
                {
                    0 => "",
                    1 => "--dport " + destPorts.First(),
                    _ => "-m multiport --dports " + string.Join(",", destPorts)
                };

                var srcPorts = GetPorts(cfg, rule.Source);
                var sport = srcPorts.Length switch
                {
                    0 => "",
                    1 => "--sport " + srcPorts.First(),
                    _ => "-m multiport --sports " + string.Join(",", srcPorts)
                };

                var destAddrs = GetAddresses(cfg, rule.Destination);
                var destinations = destAddrs.Length > 0
                    ? destAddrs.Select(d => "-d " + d).ToArray()
                    : [""];

                var srcAddrs = GetAddresses(cfg, rule.Source);
                var sources = srcAddrs.Length > 0
                    ? srcAddrs.Select(d => "-s " + d).ToArray()
                    : [""];

                foreach (var source in sources)
                {
                    foreach (var destination in destinations)
                    {
                        foreach (var proto in protos)
                        {
                            yield return $"-A {chain} {direction} {nic} {source} {destination} {proto} {sport} {dport} -j {action}";
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

        private static List<string> GetProtos(FirewallRule rule)
        {
            var protos = new List<string>();
            if (string.IsNullOrEmpty(rule.Protocol) || rule.Protocol == "all")
            {
                protos.Add("");
            }
            else if (rule.Protocol == "tcp_udp")
            {
                protos.Add("-p tcp");
                protos.Add("-p udp");
            }
            else if (rule.Protocol == "tcp" || rule.Protocol == "udp")
            {
                protos.Add("-p " + rule.Protocol);
            }
            else if (rule.Protocol == "ipv6-icmp")
            {
                var proto = "-p " + rule.Protocol;
                if (!string.IsNullOrEmpty(rule.Icmpv6?.TypeName) && rule.Icmpv6?.TypeName != "any")
                {
                    proto += " --icmpv6-type " + rule.Icmpv6?.TypeName;
                }
                protos.Add(proto);
            }
            return protos;
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
                return cfg.Firewall.Groups.AddressGroupsV6[name].AddressesV6.ToArray();
            }
            else if (!string.IsNullOrEmpty(destination?.Address))
            {
                return [destination.Address];
            }
            return [];
        }
    }
}
