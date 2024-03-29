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
                // iptables -t nat -F
                var iptables = new Process
                {
                    StartInfo =
                    {
                        FileName = "iptables",
                        Arguments = "-t nat -F"
                    }
                };
                iptables.Start();
                await iptables.WaitForExitAsync();

                // ufw reload
                var ufw = new Process
                {
                    StartInfo =
                    {
                        FileName = "ufw",
                        Arguments = "reload",
                    }
                };
                ufw.Start();
                await ufw.WaitForExitAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Can't reload firewall");
            }
        }
    }
}
