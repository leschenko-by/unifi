using Moq.AutoMock;
using Unifi.Gateway.Common.Services;
using Unifi.Gateway.Models;

namespace Unifi.Tests
{
    public class SystemConfigurationServiceTests
    {
        private readonly AutoMocker mocker = new();
        private readonly SystemConfigurationV1Service service;

        public SystemConfigurationServiceTests()
        {
            service = mocker.CreateInstance<SystemConfigurationV1Service>();
        }

        [Fact]
        public async Task ApplyAsync()
        {
            // Arrange
            var configuration = new Configuration();

            var systemCfg = File.ReadAllText(Path.Combine("dump", "system.json"));

            // Act
            await service.ApplyAsync(systemCfg, configuration);

            // Assert
            Assert.Equal("8.8.8.8", configuration.EchoServer);
        }
    }
}
