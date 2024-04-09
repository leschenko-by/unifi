using Moq;
using Moq.AutoMock;
using Unifi.Gateway.Common.Interfaces;
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
            mocker.Use<IFirewallBuilderIPv4>(mocker.CreateInstance<FirewallBuilderIPv4>());
            mocker.Use<IFirewallBuilderIPv6>(mocker.CreateInstance<FirewallBuilderIPv6>());

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
            Assert.Equal("134.17.24.1", configuration.EchoServer);

            mocker.GetMock<IFirewallService>().Verify(t => t.ApplyIPv4RulesAsync(It.IsAny<string>()));
            mocker.GetMock<IFirewallService>().Verify(t => t.ApplyIPv6RulesAsync(It.IsAny<string>()));
        }
    }
}
