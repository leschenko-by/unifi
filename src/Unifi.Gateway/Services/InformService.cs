using Microsoft.Extensions.Options;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.Json.Nodes;
using Unifi.Gateway.Common.Interfaces;
using Unifi.Gateway.Json;
using Unifi.Gateway.Options;

namespace Unifi.Gateway.Services
{
    public class InformService(
        IRequestEncoder encoder,
        IRequestDecoder decoder,
        IUnifiDevice device,
        IOptions<GeneralServiceOptions> options,
        ILogger<InformService> logger,
        IHttpClientFactory httpClientFactory) : BackgroundService()
    {
        private readonly IRequestEncoder encoder = encoder;
        private readonly IRequestDecoder decoder = decoder;
        private readonly IUnifiDevice device = device;
        private readonly IOptions<GeneralServiceOptions> options = options;
        private readonly ILogger<InformService> logger = logger;
        private readonly IHttpClientFactory httpClientFactory = httpClientFactory;

        protected override async Task ExecuteAsync(CancellationToken token)
        {
            var httpClient = httpClientFactory.CreateClient();

            var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
            while (!token.IsCancellationRequested)
            {
                await device.ReloadConfigsAsync();

                //var (informUrl, key, adopted) = GetAdoptOptions();
                var informUrl = device.InformUrl;
                var key = device.Key;
                if (!string.IsNullOrEmpty(informUrl))
                {
                    try
                    {
                        logger.LogInformation("Sending inform to {InformUrl}", informUrl);

                        var message = device.GetInformMessage();
                        var data = Encoding.UTF8.GetBytes(message);
                        var body = encoder.Encode(data, key, device.MacAddress, true, EncryptMode.Gcm);

                        var request = CreateRequestMessage(informUrl, body);
                        var reponse = await httpClient.SendAsync(request, token);
                        reponse.EnsureSuccessStatusCode();
                        body = await reponse.Content.ReadAsByteArrayAsync(token);

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

        private string GetInformPayload(bool adopted)
        {
            var json = new JsonObject
            {
                ["discovery_response"] = true,
                ["state"] = 1,
            };

            return json.ToString();
        }

        private static (string InformUrl, byte[] Key, bool adopted) GetAdoptOptions()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("/etc/unifi/config.json", true, true)
                .Build();

            var options = new AdoptOptions();
            config.Bind(options);

            if (string.IsNullOrEmpty(options.Key))
            {
                return (string.Empty, [], false);
            }

            return (options.InformUrl, Convert.FromHexString(options.Key), options.Adopted);
        }

        private static HttpRequestMessage CreateRequestMessage(string url, byte[] data) =>
            new(HttpMethod.Post, url)
            {
                Headers =
                {
                    UserAgent = { new("AirControl Agent", "v1.0") },
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
