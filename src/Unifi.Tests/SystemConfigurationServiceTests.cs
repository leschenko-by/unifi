using Moq;
using Moq.AutoMock;
using System.Net;
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

            var ip = IPAddress.Parse("192.168.0.1");
            var mask = IPAddress.Parse("255.255.255.0");

            var eth0 = mocker.GetMock<IEthernetInterface>();
            eth0.SetupGet(t => t.UnifiNic).Returns("eth0");
            eth0.SetupGet(t => t.IPAddress).Returns(ip);
            eth0.SetupGet(t => t.Netmask).Returns(mask);

            mocker.GetMock<INetworkInfoService>()
                .SetupGet(t => t.Interfaces)
                .Returns([eth0.Object]);

            mocker.GetMock<INetworkInfoService>()
                .Setup(t => t.GetNetwork(ip, mask))
                .Returns("192.168.0.0/24");

            // Act
            await service.ApplyAsync(systemCfg, configuration);

            // Assert
            Assert.Equal("134.17.24.1", configuration.EchoServer);

            mocker.GetMock<IFirewallService>().Verify(t => t.ApplyIPv4RulesAsync(It.IsAny<string>()));
            mocker.GetMock<IFirewallService>().Verify(t => t.ApplyIPv6RulesAsync(It.IsAny<string>()));
        }
    }
}
