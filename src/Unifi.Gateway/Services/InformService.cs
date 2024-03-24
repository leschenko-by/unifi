using System.Text;
using Unifi.Gateway.Common.Interfaces;

namespace Unifi.Gateway.Services
{
    public class InformService(
        IRequestEncoder encoder,
        IRequestDecoder decoder,
        IUnifiDevice device,
        ILogger<InformService> logger,
        IHttpClientFactory httpClientFactory) : BackgroundService()
    {
        private readonly IRequestEncoder encoder = encoder;
        private readonly IRequestDecoder decoder = decoder;
        private readonly IUnifiDevice device = device;
        private readonly ILogger<InformService> logger = logger;
        private readonly IHttpClientFactory httpClientFactory = httpClientFactory;

        protected override async Task ExecuteAsync(CancellationToken token)
        {
            var httpClient = httpClientFactory.CreateClient();

            var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));
            while (!token.IsCancellationRequested)
            {
                device.LoadConfigration();

                var informUrl = device.InformUrl;
                var key = device.Key;
                if (!string.IsNullOrEmpty(informUrl))
                {
                    try
                    {
                        logger.LogInformation("Sending inform to {InformUrl}", informUrl);

                        var message = device.GetInformMessage();
                        logger.LogInformation("Sending inform message: {Message}", message);

                        var data = Encoding.UTF8.GetBytes(message);
                        var body = encoder.Encode(data, key, device.MacAddress, true, EncryptMode.Cbc);

                        var request = CreateRequestMessage(informUrl, body);
                        var reponse = await httpClient.SendAsync(request, token);
                        body = await reponse.Content.ReadAsByteArrayAsync(token);

                        logger.LogInformation("Received message size: {Size}", body.Length);
                        //Directory.CreateDirectory("/usr/src/unifi/logs");
                        //var logFile = Path.Combine("/usr/src/unifi/logs", DateTime.Now.ToString("yyyy-MM-ddTHH-mm-ss") + ".json");
                        //await File.WriteAllBytesAsync(logFile, body);

                        reponse.EnsureSuccessStatusCode();

                        data = decoder.Decode(body, key);
                        var json = Encoding.UTF8.GetString(data);

                        await device.UpdateAsync(json);
                        logger.LogInformation("Received inform response: {Response}", json);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to send inform to {InformUrl}", informUrl);
                    }
                }

                await timer.WaitForNextTickAsync(token);
            }
        }

        private static HttpRequestMessage CreateRequestMessage(string url, byte[] data) =>
            new(HttpMethod.Post, url)
            {
                Headers =
                {
                    { "User-Agent", "AirControl Agent v1.0" },
                },
                Content = new ByteArrayContent(data)
                {
                    Headers =
                    {
                        ContentType = new("application/x-binary"),
                    },
                },
            };
    }
}
