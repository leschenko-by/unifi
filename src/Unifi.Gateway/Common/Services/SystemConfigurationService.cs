using System.Text;
using System.Text.Json;
using Unifi.Gateway.Common.Interfaces;
using Unifi.Gateway.Json;
using Unifi.Gateway.Models;

namespace Unifi.Gateway.Common.Services
{
    public class SystemConfigurationService : ISystemConfigurationService
    {
        public readonly IUfwService ufw;

        public SystemConfigurationService(IUfwService ufw)
        {
            this.ufw = ufw;
        }

        public async Task ApplyAsync(string systemCfg)
        {
            Directory.CreateDirectory("/etc/unifi");
            await File.WriteAllTextAsync("/etc/unifi/system.json", systemCfg);

            var cfg = JsonSerializer.Deserialize(systemCfg, SourceGenerationContext.Default.SystemConfiguration);
            if (cfg is null)
            {
                return;
            }

            await ApplyPortForwardingAsync(cfg);
        }

        private async Task ApplyPortForwardingAsync(SystemConfiguration cfg)
        {
            await Task.Yield();
            var rules = cfg.Unifi.PortForwarding.Rules;
            if (cfg.Unifi.PortForwarding.Status != "enable")
            {
                rules.Clear();
            }

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
                        foreach (var (key, value) in rules)
                        {
                            output.AppendLine($"# {key} => {value}");
                            var options = value.Split(",").Select(t => t.Split("="))
                                .ToDictionary(t => t[0], t => t[1]);

                            var tcp = options["tcp"] == "1";
                            var udp = options["udp"] == "1";

                            var dst_port = options["dst_port"].Trim('\'');
                            var fwd_port = options["fwd_port"].Trim('\'');

                            var proto = (tcp && udp ? "" : tcp ? "-p tcp" : udp ? "-p udp" : "");
                            output.AppendLine($"-A PREROUTING -i eth0 {proto} --dport {dst_port} -j DNAT --to-destination {options["fwd"]}:{fwd_port}");
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

            await File.WriteAllTextAsync("/etc/ufw/before.rules", output.ToString().ReplaceLineEndings());

            await ufw.ReloadAsync();
        }
    }
}
