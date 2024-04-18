using Microsoft.Extensions.Options;
using Unifi.Gateway.Common.Interfaces;
using Unifi.Gateway.Common.Models;

namespace Unifi.Gateway.Common.Services
{
    public class UnifiProtocol: IUnifiProtocol
    {
        private readonly IRequestEncoder encoder;
        private readonly IRequestDecoder decoder;
        private readonly INetworkInfoService network;
        private readonly IOptions<GeneralServiceOptions> serviceOptions;
        private readonly HttpClient httpClient;

        public UnifiProtocol(
            IRequestEncoder encoder, 
            IRequestDecoder decoder, 
            INetworkInfoService network,
            IOptions<GeneralServiceOptions> serviceOptions,
            IHttpClientFactory httpClientFactory)
        {
            this.encoder = encoder;
            this.decoder = decoder;
            this.network = network;
            this.serviceOptions = serviceOptions;
            httpClient = httpClientFactory.CreateClient();
        }

        public async Task<byte[]> SendRequestAsync(string informUrl, byte[] key, byte[] data, CancellationToken token)
        {
            var eth = network.Interfaces[serviceOptions.Value.DiscoveryPortId] 
                ?? throw new InvalidOperationException("Network interface is not ready");
            var body = encoder.Encode(data, key, eth.MacAddress, CompressMode.Zlib, EncryptMode.Gcm);
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
