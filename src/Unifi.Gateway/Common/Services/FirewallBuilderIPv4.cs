using System.Text;
using Unifi.Gateway.Common.Interfaces;
using Unifi.Gateway.Models.V1;

namespace Unifi.Gateway.Common.Services
{
    public class FirewallBuilderIPv4 : IFirewallBuilderIPv4
    {
        public string Build(SystemConfiguration cfg)
        {
            const string wan = "eth0";
            const string lan = "eth1";
            var rules = new StringBuilder();

            AppendNatRules(cfg, rules, wan);
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

                :unifi-before-input - [0:0]
                :unifi-user-input - [0:0]
                :unifi-after-input - [0:0]

                :unifi-before-forward - [0:0]
                :unifi-user-forward - [0:0]
                :unifi-after-forward - [0:0]

                :unifi-before-output - [0:0]
                :unifi-user-output - [0:0]
                :unifi-after-output - [0:0]

                -A INPUT -j unifi-before-input
                -A INPUT -j unifi-user-input
                -A INPUT -j unifi-after-input
                -A FORWARD -j unifi-before-forward
                -A FORWARD -j unifi-user-forward
                -A FORWARD -j unifi-after-forward
                -A OUTPUT -j unifi-before-output
                -A OUTPUT -j unifi-user-output
                -A OUTPUT -j unifi-after-output

                -A unifi-before-input -i lo -j ACCEPT
                -A unifi-before-output -o lo -j ACCEPT
                -A unifi-before-input -p udp -m udp --sport 67 --dport 68 -j ACCEPT
                -A unifi-before-input -i {lan} -m conntrack --ctstate RELATED,ESTABLISHED -j ACCEPT
                -A unifi-before-input -i {lan} -m conntrack --ctstate INVALID -j DROP
                -A unifi-after-output -o {lan} -j ACCEPT

                """);

            //todo: get values from config
            rules.AppendLine($"""
                # VPN
                -A unifi-before-forward -s 172.26.16.0/24 -m policy --dir in --pol ipsec --proto esp -j ACCEPT
                -A unifi-before-forward -d 172.26.16.0/24 -m policy --dir out --pol ipsec --proto esp -j ACCEPT
                -A unifi-before-forward -s 192.168.128.0/20 -m policy --dir in --pol ipsec --proto esp -j ACCEPT
                -A unifi-before-forward -d 192.168.128.0/20 -m policy --dir out --pol ipsec --proto esp -j ACCEPT
                -A unifi-before-forward -s 10.208.0.0/16 -m policy --dir in --pol ipsec --proto esp -j ACCEPT
                -A unifi-before-forward -d 10.208.0.0/16 -m policy --dir out --pol ipsec --proto esp -j ACCEPT
                -A unifi-before-forward -s 10.110.0.0/20 -m policy --dir in --pol ipsec --proto esp -j ACCEPT
                -A unifi-before-forward -d 10.110.0.0/20 -m policy --dir out --pol ipsec --proto esp -j ACCEPT

                """);

            AppendRules(rules, BuildFilters(cfg, "WAN_LOCAL", wan, "unifi-user-input", true));
            AppendRules(rules, BuildFilters(cfg, "WAN_IN", wan, "unifi-user-forward", true));
            AppendRules(rules, BuildFilters(cfg, "WAN_OUT", wan, "unifi-user-output", false));
            AppendRules(rules, BuildFilters(cfg, "LAN_LOCAL", lan, "unifi-user-input", true));
            AppendRules(rules, BuildFilters(cfg, "LAN_IN", lan, "unifi-user-forward", true));
            AppendRules(rules, BuildFilters(cfg, "LAN_OUT", lan, "unifi-user-forward", false));

            rules.AppendLine("COMMIT");
            rules.AppendLine();
        }

        private static IEnumerable<string> BuildFilters(SystemConfiguration cfg, string name, string nic, string chain, bool input)
        {
            yield return $"# {name}";
            foreach (var rule in cfg.Firewall.Names[name].Rules.Select(t => t.Value))
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
                    var direction = input ? "-i" : "-o";
                    yield return $"-A {chain} {direction} {nic} -m conntrack --ctstate {string.Join(",", states)} -j {action}";
                    continue;
                }

                var protos = GetProtos(rule);

                var destPorts = GetPorts(cfg, rule.Destination);
                var port = destPorts.Length switch
                {
                    0 => "",
                    1 => "--dport " + destPorts.First(),
                    _ => "-m multiport --dports " + string.Join(",", destPorts)
                };

                var destAddrs = GetAddresses(cfg, rule.Destination);
                var destinations = destAddrs.Length > 0
                    ? destAddrs.Select(d => "-d " + d).ToArray()
                    : [input ? "" : $"-o {nic}"];

                var srcAddrs = GetAddresses(cfg, rule.Source);
                var sources = srcAddrs.Length > 0
                    ? srcAddrs.Select(d => "-s " + d).ToArray()
                    : [input ? $"-i {nic}" : ""];

                foreach (var source in sources)
                {
                    foreach (var destination in destinations)
                    {
                        foreach (var proto in protos)
                        {
                            yield return $"-A {chain} {source} {destination} {proto} {port} -j {action}";
                        }
                    }
                }
            }

            if (cfg.Firewall.Names[name].DefaultAction != "drop")
            {
                var direction = input ? "-i" : "-o";
                yield return $"-A {chain} {direction} {nic} -j {cfg.Firewall.Names[name].DefaultAction.ToUpper()}";
            }
            yield return string.Empty;
        }

        private static string[] GetAddresses(SystemConfiguration cfg, FirewallRuleDestination? destination)
        {
            var name = destination?.Group?.AddressGroup;
            if (!string.IsNullOrEmpty(name))
            {
                return cfg.Firewall.Groups.AddressGroups[name].Addresses.ToArray();
            }
            else if (!string.IsNullOrEmpty(destination?.Address))
            {
                return [destination.Address];
            }
            return [];
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

        private static List<string> GetProtos(FirewallRule rule)
        {
            var protos = new List<string>();
            if (string.IsNullOrEmpty(rule.Protocol))
            {
                protos.Add("");
            }
            else if (rule.Protocol == "tcp_udp" || rule.Protocol == "all")
            {
                protos.Add("-p tcp");
                protos.Add("-p udp");
            }
            else if (rule.Protocol == "tcp" || rule.Protocol == "udp")
            {
                protos.Add("-p " + rule.Protocol);
            }
            else if (rule.Protocol == "icmp")
            {
                var proto = "-p " + rule.Protocol;
                if (!string.IsNullOrEmpty(rule.Icmp?.TypeName) && rule.Icmp?.TypeName != "any")
                {
                    proto += " --icmp-type " + rule.Icmp?.TypeName;
                }
                protos.Add(proto);
            }
            return protos;
        }

        private static void AppendNatRules(SystemConfiguration cfg, StringBuilder rules, string wan)
        {
            rules.AppendLine($"""
                *nat
                :PREROUTING ACCEPT [0:0]
                :INPUT ACCEPT [0:0]
                :OUTPUT ACCEPT [0:0]
                :POSTROUTING ACCEPT [0:0]
                """);

            var lines = GetNatTables(cfg, wan);
            AppendRules(rules, lines);

            rules.AppendLine($"""
                -A POSTROUTING -o {wan} -m policy --dir out --pol ipsec -j ACCEPT
                -A POSTROUTING -o {wan} -j MASQUERADE
                COMMIT

                """);
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

        private static IEnumerable<string> GetNatTables(SystemConfiguration cfg, string wan)
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

                var sourceRule = source == "0.0.0.0" ? "-i " + wan : "-s " + source;

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
    }
}
