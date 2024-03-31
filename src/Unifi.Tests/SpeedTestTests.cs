using Unifi.SpeedTest;

namespace Unifi.Tests
{
    public class SpeedTestTests
    {
        private readonly SpeedTestClient client = new();

        [Fact]
        public async void GetSettings()
        {
            var settings = await client.GetServerAsync();
            Assert.NotNull(settings);
        }

        [Fact]
        public async Task GetServers()
        {
            var server = await client.GetServerAsync();
            Assert.NotNull(server);
        }

        [Fact]  
        public async Task GetLatency()
        {
            var server = await client.GetServerAsync();
            var latency = await client.TestServerLatencyAsync(server);
            Assert.True(latency > 0);
        }

        [Fact]
        public async Task GetDownloadSpeed()
        {
            var server = await client.GetServerAsync();
            var speed = await client.TestDownloadSpeedAsync(server, 8);
            Assert.True(speed > 0);
        }

        [Fact]
        public async Task GetUploadSpeed()
        {
            var server = await client.GetServerAsync();
            var speed = await client.TestUploadSpeedAsync(server, 8);
            Assert.True(speed > 0);
        }
    }
}
