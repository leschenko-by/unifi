using System.Text;
using Unifi.Gateway.Common.Interfaces;

namespace Unifi.Gateway.Services
{
    public class InformService(
        IUnifiProtocol protocol,
        IUnifiDevice device,
        ILogger<InformService> logger) : BackgroundService()
    {
        private readonly IUnifiProtocol protocol = protocol;
        private readonly IUnifiDevice device = device;
        private readonly ILogger<InformService> logger = logger;

        protected override async Task ExecuteAsync(CancellationToken token)
        {
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
                }

                await Task.Delay(device.Interval, token);
            }
        }
    }
}
