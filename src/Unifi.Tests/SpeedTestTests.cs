using Unifi.SpeedTest;

namespace Unifi.Tests
{
    public class SpeedTestTests
    {
        private readonly SpeedTestClient client = new();

        [Fact]
        public async void GetSettings()
        {
            var settings = await client.GetSettingsAsync();
            Assert.NotNull(settings);
        }

        [Fact]
        public async Task GetServers()
        {
            var settings = await client.GetSettingsAsync();
            var server = await settings.GetServer();
            Assert.NotNull(server);
        }

        [Fact]  
        public async Task GetLatency()
        {
            var settings = await client.GetSettingsAsync();
            var server = await settings.GetServer();
            var latency = await client.TestServerLatencyAsync(server);
            Assert.True(latency > 0);
        }
    }
}
