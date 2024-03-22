using Microsoft.Extensions.Options;
using Unifi.Gateway.Json;

namespace Unifi.Gateway.Services
{
    public class InformService : BackgroundService
    {
        private readonly IServiceProvider serviceProvider;
        private readonly ILogger<InformService> logger;

        public InformService(IServiceProvider serviceProvider, ILogger<InformService> logger) : base()
        {
            this.serviceProvider = serviceProvider;
            this.logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken token)
        {
            var periodic = new PeriodicTimer(TimeSpan.FromSeconds(1));

            while (!token.IsCancellationRequested)
            {
                var config = new ConfigurationBuilder()
                    .AddJsonFile("/etc/unifi/config.json", true, true)
                    .Build();

                var options = new AdoptOptions();
                config.Bind(options);

                if (!string.IsNullOrEmpty(options.InformUrl))
                {
                    logger.LogInformation("Sending inform to {InformUrl}", options.InformUrl);
                }

                await periodic.WaitForNextTickAsync(token);
            }
        }
    }
}
