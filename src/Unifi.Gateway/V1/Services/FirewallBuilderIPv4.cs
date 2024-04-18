using System.Text;
using System.Text.Json.Nodes;
using Unifi.Gateway.Common.Interfaces;
using Unifi.Gateway.V1.Interfaces;
using Unifi.Gateway.V1.Models;

namespace Unifi.Gateway.V1.Services
{
    public class FirewallBuilderIPv4(INetworkInfoService network, IFileReader fileReader) : IFirewallBuilderIPv4
    {
        private readonly INetworkInfoService network = network;
        private readonly IFileReader fileReader = fileReader;

        public async Task<string> BuildAsync(SystemConfiguration cfg)
        {
            const string wan = "eth0";
            const string lan = "eth1";
            var rules = new StringBuilder();

            await AppendNatRulesAsync(cfg, rules, wan);
            await AppendFilterRulesAsync(cfg, rules, wan, lan);

            return rules.ToString();
        }

        private async Task AppendFilterRulesAsync(SystemConfiguration cfg, StringBuilder rules, string wan, string lan)
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

                :unifi-log-accept - [0:0]
                :unifi-log-reject - [0:0]
                :unifi-log-drop - [0:0]
                
                -A INPUT -j unifi-before-input
                -A INPUT -j unifi-user-input
                -A INPUT -j unifi-after-input
                -A FORWARD -j unifi-before-forward
                -A FORWARD -j unifi-user-forward
                -A FORWARD -j unifi-after-forward
                -A OUTPUT -j unifi-before-output
                -A OUTPUT -j unifi-user-output
                -A OUTPUT -j unifi-after-output

                -A unifi-log-accept -j LOG --log-prefix="[unifi accept] "
                -A unifi-log-accept -j ACCEPT

                -A unifi-log-reject -j LOG --log-prefix="[unifi reject] "
                -A unifi-log-reject -j REJECT

                -A unifi-log-drop -j LOG --log-prefix="[unifi drop] "
                -A unifi-log-drop -j DROP

                -A unifi-before-input -i lo -j ACCEPT
                -A unifi-before-output -o lo -j ACCEPT
                -A unifi-before-input -p udp -m udp --sport 67 --dport 68 -j ACCEPT
                -A unifi-before-input -i {lan} -m conntrack --ctstate RELATED,ESTABLISHED -j ACCEPT
                -A unifi-before-input -i {lan} -m conntrack --ctstate INVALID -j DROP
                -A unifi-after-output -o {lan} -j ACCEPT

                """);

            var custom = await fileReader.ReadAsync("/etc/iptables/custom-filter.v4");
            if (!string.IsNullOrEmpty(custom))
            {
                rules.AppendLine("# Custom");
                rules.AppendLine(custom);
                rules.AppendLine();
            }

            AddVpnRules(cfg, rules, "remote_user_vpn_network", "# Point-to-Point VPNs");
            AddVpnRules(cfg, rules, "remote_site_vpn_network", "# Site-to-Site VPNs");

            AddVpnToInternetRules(cfg, rules, "remote_user_vpn_network", wan);
            AddVpnToSitesRules(cfg, rules, "remote_user_vpn_network", "remote_site_vpn_network");

            AppendRules(rules, BuildFilters(cfg, "WAN_LOCAL", wan, "unifi-user-input", true));
            AppendRules(rules, BuildFilters(cfg, "WAN_IN", wan, "unifi-user-forward", true));
            AppendRules(rules, BuildFilters(cfg, "WAN_OUT", wan, "unifi-user-output", false));
            AppendRules(rules, BuildFilters(cfg, "LAN_LOCAL", lan, "unifi-user-input", true));
            AppendRules(rules, BuildFilters(cfg, "LAN_IN", lan, "unifi-user-forward", true));
            AppendRules(rules, BuildFilters(cfg, "LAN_OUT", lan, "unifi-user-forward", false));

            rules.AppendLine("COMMIT");
            rules.AppendLine();
        }

        private static void AddVpnToInternetRules(SystemConfiguration cfg, StringBuilder rules, string vpnNetworkGroup, string wan)
        {
            rules.AppendLine("# Allow internet access from VPN");
            foreach (var p2p in cfg.Firewall.Groups.NetworkGroups[vpnNetworkGroup].Networks)
            {
                rules.AppendLine($"-A unifi-before-forward -s {p2p} -o {wan} -j ACCEPT");
            }
            rules.AppendLine();
        }

        private static void AddVpnToSitesRules(SystemConfiguration cfg, StringBuilder rules, string vpnNetworkGroup, string siteNetworkGroup)
        {
            rules.AppendLine("# Allow access to Site-to-Site from VPN");
            foreach (var p2p in cfg.Firewall.Groups.NetworkGroups[vpnNetworkGroup].Networks)
            {
                foreach (var s2s in cfg.Firewall.Groups.NetworkGroups[siteNetworkGroup].Networks)
                {
                    rules.AppendLine($"-A unifi-before-forward -s {p2p} -d {s2s} -j ACCEPT");
                    rules.AppendLine($"-A unifi-before-forward -d {p2p} -s {s2s} -j ACCEPT");
                }
            }
            rules.AppendLine();
        }

        private static void AddVpnRules(SystemConfiguration cfg, StringBuilder rules, string networkGroup, string comment)
        {
            rules.AppendLine(comment);
            foreach (var network in cfg.Firewall.Groups.NetworkGroups[networkGroup].Networks)
            {
                rules.AppendLine($"-A unifi-before-forward -s {network} -m policy --pol ipsec --dir in -p esp -j ACCEPT");
                rules.AppendLine($"-A unifi-before-forward -d {network} -m policy --pol ipsec --dir out -p esp -j ACCEPT");
            }
            rules.AppendLine();
        }

        private IEnumerable<string> BuildFilters(SystemConfiguration cfg, string name, string nic, string chain, bool input)
        {
            yield return $"# {name}";
            foreach (var rule in cfg.Firewall.Names[name].Rules.OrderBy(t => Convert.ToInt32(t.Key)).Select(t => t.Value))
            {
                var action = rule.Action.ToUpper();
                if (rule.Log == "enable")
                {
                    action = "unifi-log-" + action.ToLower();
                }

                var state = GetStates(rule);

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
                    _ => [input ? "" : $"-o {nic}"]
                };

                var sources = GetAddresses(cfg, rule.Source) switch
                {
                    var t when t.Length > 0 => t.Select(d => "-s " + d).ToArray(),
                    _ => [input ? $"-i {nic}" : ""]
                };

                var sourceMac = rule.Source?.MacAddress ?? string.Empty;
                if (!string.IsNullOrEmpty(sourceMac))
                {
                    sourceMac = "--mac-source " + sourceMac;
                }

                foreach (var (source, sport) in sources.Zip(sports))
                {
                    foreach (var (destination, dport) in destinations.Zip(dports))
                    {
                        foreach (var proto in protos)
                        {
                            yield return $"-A {chain} {source} {sourceMac} {destination} {proto} {sport} {dport} {state} -j {action}";
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

        private static string GetStates(FirewallRule rule)
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

        private string[] GetAddresses(SystemConfiguration cfg, FirewallRuleDestination? destination)
        {
            var name = destination?.Group?.AddressGroup;
            if (!string.IsNullOrEmpty(name))
            {
                if (name.StartsWith("ADDRv4_"))
                {
                    var nic = name["ADDRv4_".Length..];
                    var eth = network.Interfaces.FirstOrDefault(t => t?.UnifiNic == nic);
                    if (eth is not null)
                    {
                        return [eth.IPAddress.ToString()];
                    }
                }
                else if (name.StartsWith("NETv4_"))
                {
                    var nic = name["NETv4_".Length..];
                    var eth = network.Interfaces.FirstOrDefault(t => t?.UnifiNic == nic);
                    if (eth is not null)
                    {
                        return [network.GetNetwork(eth.IPAddress, eth.Netmask)];
                    }
                }
                else if (cfg.Firewall.Groups.AddressGroups.TryGetValue(name, out var group))
                {
                    return [.. group.Addresses];
                }
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

        private static List<string> GetProtocols(FirewallRule rule)
        {
            var protocol = rule.Protocol;
            if (string.IsNullOrEmpty(protocol))
            {
                return [""];
            }

            var opposite = protocol.StartsWith('!');
            var icmpType = rule.Icmp?.TypeName;
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
                else if (protocol == "tcp" || protocol == "udp" || protocol == "esp")
                {
                    return ["-p " + protocol];
                }
                else if (protocol == "icmp")
                {
                    var proto = "-p icmp";
                    if (!string.IsNullOrEmpty(icmpType) && icmpType != "any")
                    {
                        proto += " --icmp-type " + icmpType;
                    }
                    return [proto];
                }
                return [];
            }
        }

        private async Task AppendNatRulesAsync(SystemConfiguration cfg, StringBuilder rules, string wan)
        {
            rules.AppendLine($"""
                *nat
                :PREROUTING ACCEPT [0:0]
                :INPUT ACCEPT [0:0]
                :OUTPUT ACCEPT [0:0]
                :POSTROUTING ACCEPT [0:0]

                """);

            var custom = await fileReader.ReadAsync("/etc/iptables/custom-nat.v4");
            if (!string.IsNullOrEmpty(custom))
            {
                rules.AppendLine("# Custom");
                rules.AppendLine(custom);
                rules.AppendLine();
            }

            var lines = GetNatTables(cfg, wan);
            AppendRules(rules, lines);

            rules.AppendLine("# IPSec");
            rules.AppendLine($"-A POSTROUTING -o {wan} -m policy --dir out --pol ipsec -j ACCEPT");
            rules.AppendLine();

            AppendRules(rules, BuildMasqueradeRules(cfg));

            rules.AppendLine("COMMIT");
            rules.AppendLine();
        }

        private IEnumerable<string> BuildMasqueradeRules(SystemConfiguration cfg)
        {
            foreach (var (_, rule) in cfg.Service.Nat.Rules)
            {
                if (rule.Type != "masquerade") continue;

                var eth = network.Interfaces.FirstOrDefault(t => t?.UnifiNic == rule.OutboundInterface);
                if (eth is null) continue;

                if (!cfg.Firewall.Groups.NetworkGroups.TryGetValue(rule.Source.Group.NetworkGroup, out var group)) continue;

                if (group.Networks.Count == 0) continue;

                yield return "# " + rule.Description;
                foreach (var network in group.Networks)
                {
                    yield return $"-A POSTROUTING -s {network} -o {eth.LocalNic} -j MASQUERADE";
                }
            }

            yield return "";
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

            yield return "# Port forwarding";
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

            yield return "";
        }
    }
}
