using System.Text;
using Unifi.Gateway.Common.Interfaces;

namespace Unifi.Gateway.Services
{
    public class InformService : BackgroundService
    {
        private readonly IUnifiProtocol protocol;
        private readonly IUnifiDevice device;
        private readonly IConnectRequest connectRequest;
        private readonly ILogger<InformService> logger;

        public InformService(string deviceName, IServiceProvider serviceProvider)
        {
            protocol = serviceProvider.GetRequiredService<IUnifiProtocol>();
            connectRequest = serviceProvider.GetRequiredService<IConnectRequest>();
            logger = serviceProvider.GetRequiredService<ILogger<InformService>>();

            device = serviceProvider.GetRequiredKeyedService<IUnifiDevice>(deviceName);
        }

        protected override async Task ExecuteAsync(CancellationToken token)
        {
            int broadcastIndex = 0;

            while (!token.IsCancellationRequested)
            {
                device.LoadConfigration();

                var informUrl = device.InformUrl;
                var key = device.Key;
                if (!string.IsNullOrEmpty(informUrl) && key.Length != 0)
                {
                    try
                    {
                        logger.LogInformation("Sending inform to {InformUrl}", informUrl);

                        var message = await device.GetInformMessageAsync();
                        logger.LogInformation("Sending inform message: {Message}", message);
                        var requestMessageData = Encoding.UTF8.GetBytes(message);

                        var responseMessageData = await protocol.SendRequestAsync(informUrl, key, requestMessageData, token);

                        var json = Encoding.UTF8.GetString(responseMessageData);
                        await device.ParseResponseAsync(json);
                        logger.LogInformation("Received inform response: {Response}", json);

                        device.SaveConfigration();
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to send inform to {InformUrl}", informUrl);
                    }

                    var timeLimit = DateTime.UtcNow + device.Interval;
                    while (DateTime.UtcNow < timeLimit)
                    {
                        await Task.Delay(500, token);
                        if (connectRequest.IsRequestPending())
                        {
                            break;
                        }
                    }
                }
                else
                {
                    await device.SendDiscoveryAsync(broadcastIndex);
                    await Task.Delay(TimeSpan.FromSeconds(1), token);

                    broadcastIndex = (broadcastIndex + 1) % 20;
                }
            }
        }
    }
}
