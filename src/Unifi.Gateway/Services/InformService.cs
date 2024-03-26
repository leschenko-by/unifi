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
        private readonly HttpClient httpClient = httpClientFactory.CreateClient();

        protected override async Task ExecuteAsync(CancellationToken token)
        {
            var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
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

                        var responseMessageData = await SendRequestAsync(informUrl, key, requestMessageData, token);

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

                await timer.WaitForNextTickAsync(token);
            }
        }

        private async Task<byte[]> SendRequestAsync(string informUrl, byte[] key, byte[] data, CancellationToken token)
        {
            var body = encoder.Encode(data, key, device.MacAddress, CompressMode.Zlib, EncryptMode.Gcm);
            using var request = CreateRequestMessage(informUrl, body);
            using var reponse = await httpClient.SendAsync(request, token);
            body = await reponse.EnsureSuccessStatusCode().Content.ReadAsByteArrayAsync(token);
            return decoder.Decode(body, key);
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
