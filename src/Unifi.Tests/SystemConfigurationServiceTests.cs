using Microsoft.Extensions.Options;
using Moq;
using Moq.AutoMock;
using System.Net;
using System.Net.Sockets;
using Unifi.Gateway.Common.Interfaces;
using Unifi.Gateway.Common.Models;
using Unifi.Gateway.V1.Interfaces;
using Unifi.Gateway.V1.Services;

namespace Unifi.Tests
{
    public class SystemConfigurationServiceTests
    {
        private readonly AutoMocker mocker = new();
        private readonly SystemConfigurationV1Service service;

        public SystemConfigurationServiceTests()
        {
            mocker.Use(Options.Create(new FirewallOptions()));

            mocker.Use<IFirewallBuilderIPv4>(mocker.CreateInstance<FirewallBuilderIPv4>());
            mocker.Use<IFirewallBuilderIPv6>(mocker.CreateInstance<FirewallBuilderIPv6>());

            service = mocker.CreateInstance<SystemConfigurationV1Service>();
        }

        [Fact]
        public async Task ApplyAsync()
        {
            // Arrange
            var configuration = new Configuration();

            var systemCfg = File.ReadAllText(Path.Combine("dump", "system2.json"));

            var lanip = IPAddress.Parse("192.168.0.1");
            var wanip = IPAddress.Parse("134.17.26.13");
            var mask = IPAddress.Parse("255.255.255.0");

            var eth0 = mocker.GetMock<IEthernetInterface>();
            eth0.SetupGet(t => t.UnifiNic).Returns("eth0");
            eth0.SetupGet(t => t.LocalNic).Returns("eth1");
            eth0.SetupGet(t => t.IPAddress).Returns(lanip);
            eth0.SetupGet(t => t.Netmask).Returns(mask);

            var eth2 = mocker.GetMock<IEthernetInterface>();
            eth2.SetupGet(t => t.UnifiNic).Returns("eth2");
            eth2.SetupGet(t => t.LocalNic).Returns("eth0");
            eth2.SetupGet(t => t.IPAddress).Returns(wanip);
            eth2.SetupGet(t => t.Netmask).Returns(mask);

            mocker.GetMock<INetworkInfoService>()
                .SetupGet(t => t.Interfaces)
                .Returns([eth0.Object, null, eth2.Object]);

            mocker.GetMock<INetworkInfoService>()
                .Setup(t => t.GetNetwork(lanip, mask))
                .Returns("192.168.0.0/24");

            mocker.GetMock<IFirewallService>()
                .Setup(t => t.ApplyIPv4RulesAsync(It.IsAny<string>()))
                .Callback<string>(rules =>
                {
                    Assert.True(rules.Length > 0);
                });
            mocker.GetMock<IFirewallService>()
                .Setup(t => t.ApplyIPv6RulesAsync(It.IsAny<string>()))
                .Callback<string>(rules =>
                {
                    Assert.True(rules.Length > 0);
                });


            // Act
            await service.ApplyAsync(systemCfg, configuration);

            // Assert
            Assert.Equal("134.17.24.1", configuration.EchoServer);

            mocker.GetMock<IFirewallService>().Verify(t => t.ApplyIPv4RulesAsync(It.IsAny<string>()));
            mocker.GetMock<IFirewallService>().Verify(t => t.ApplyIPv6RulesAsync(It.IsAny<string>()));
        }
    }
}
