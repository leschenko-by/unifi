using Moq.AutoMock;
using Unifi.Gateway.Common.Services;

namespace Unifi.Tests
{
    public class SystemConfigurationServiceTests
    {
        private readonly AutoMocker mocker = new();
        private readonly SystemConfigurationService service;

        public SystemConfigurationServiceTests()
        {
            service = mocker.CreateInstance<SystemConfigurationService>();
        }

        [Fact]
        public async Task ApplyAsync()
        {
            // Arrange
            var systemCfg = File.ReadAllText(Path.Combine("dump", "system.json"));

            // Act
            await service.ApplyAsync(systemCfg);
        }
    }
}
