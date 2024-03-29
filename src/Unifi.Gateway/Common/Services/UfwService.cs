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
                var process = new Process
                {
                    StartInfo =
                    {
                        FileName = "ufw",
                        Arguments = "reload",
                    }
                };
                process.Start();
                await process.WaitForExitAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Can't reload firewall");
            }
        }
    }
}
