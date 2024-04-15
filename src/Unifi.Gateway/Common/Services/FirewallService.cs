using System.Diagnostics;
using Unifi.Gateway.Common.Interfaces;

namespace Unifi.Gateway.Common.Services
{
    public class FirewallService(ILogger<FirewallService> logger) : IFirewallService
    {
        public readonly ILogger<FirewallService> logger = logger;

        public async Task ApplyIPv4RulesAsync(string rules)
        {
            rules = rules.ReplaceLineEndings();
            var backup = await File.ReadAllTextAsync("/etc/iptables/rules.v4");
            if (rules != backup)
            {
                var timestamp = DateTime.UtcNow;
                Directory.CreateDirectory("/etc/iptables/backups");
                await File.WriteAllTextAsync($"/etc/iptables/backups/{timestamp:yyyy-MM-dd HH:mm:ss.fff}.v4", backup);

                if (await RestoreIpTablesAsync("iptables-restore", rules))
                {
                    await File.WriteAllTextAsync("/etc/iptables/rules.v4", rules);
                }
                else
                {
                    await RestoreIpTablesAsync("iptables-restore", backup);
                }
            }
        }

        public async Task ApplyIPv6RulesAsync(string rules)
        {
            rules = rules.ReplaceLineEndings();
            var backup = await File.ReadAllTextAsync("/etc/iptables/rules.v6");
            if (rules != backup)
            {
                var timestamp = DateTime.UtcNow;
                Directory.CreateDirectory("/etc/iptables/backups");
                await File.WriteAllTextAsync($"/etc/iptables/backups/{timestamp:yyyy-MM-dd HH:mm:ss.fff}.v6", backup);

                if (await RestoreIpTablesAsync("ip6tables-restore", rules))
                {
                    await File.WriteAllTextAsync("/etc/iptables/rules.v6", rules);
                }
                else
                {
                    await RestoreIpTablesAsync("ip6tables-restore", backup);
                }
            }
        }

        private async Task<bool> RestoreIpTablesAsync(string cmd, string rules)
        {
            using var process = new Process();
            process.StartInfo.FileName = cmd;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();

            var writer = process.StandardInput;
            writer.Write(rules);
            writer.Close();

            await process.WaitForExitAsync();

            var errors = process.StandardError.ReadToEnd();
            var output = process.StandardOutput.ReadToEnd();

            if (process.ExitCode != 0)
            {
                logger.LogWarning("iptables-restore has been failed with code: {exitCode}", process.ExitCode);
                logger.LogWarning(errors);
                logger.LogWarning(output);
                return false;
            }
            return true;
        }
    }
}
