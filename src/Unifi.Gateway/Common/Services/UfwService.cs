using System.Diagnostics;
using Unifi.Gateway.Common.Interfaces;

namespace Unifi.Gateway.Common.Services
{
    public class UfwService(ILogger<UfwService> logger) : IUfwService
    {
        public readonly ILogger<UfwService> logger = logger;

        public async Task ReloadAsync()
        {
            try
            {
                await ResetIpTablesAsync();
                await ReloadUfwAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Can't reload firewall");
            }

            static async Task ResetIpTablesAsync()
            {
                using var process = new Process();
                process.StartInfo.FileName = "iptables-restore";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardInput = true;
                process.Start();

                var writer = process.StandardInput;

                writer.Write($$"""
                    *nat
                    COMMIT
                    *mangle
                    COMMIT
                    *filter
                    COMMIT
                    """);

                writer.Close();

                await process.WaitForExitAsync();
            }

            static async Task ReloadUfwAsync()
            {
                using var process = new Process();
                process.StartInfo.FileName = "ufw";
                process.StartInfo.Arguments = "reload";
                process.Start();
                await process.WaitForExitAsync();
            }
        }
    }
}
