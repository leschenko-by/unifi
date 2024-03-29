using Microsoft.Extensions.Options;
using Moq.AutoMock;
using System.Net;
using Unifi.Gateway.Common.Interfaces;
using Unifi.Gateway.Devices;
using Unifi.Gateway.Json;
using Unifi.Gateway.Models;

namespace Unifi.Tests
{
    public class UnifiGatewayDeviceTests
    {
        private readonly AutoMocker mocker = new();
        private readonly UGW4Device device;

        public UnifiGatewayDeviceTests()
        {
            mocker.Use(Options.Create(new GeneralServiceOptions
            {
                LanNetworkId = "{B344A706-8387-42C3-90FF-A2AF190EC1A5}"
            }));

            mocker.GetMock<INetworkInfoService>()
                .SetupGet(t => t.LanIPAddress).Returns(IPAddress.Parse("192.168.2.254"));
            mocker.GetMock<INetworkInfoService>()
                .SetupGet(t => t.WanIPAddress).Returns(IPAddress.Parse("134.17.26.13"));
            mocker.GetMock<INetworkInfoService>()
                .SetupGet(t => t.LanNetmask).Returns(IPAddress.Parse("255.255.255.0"));
            mocker.GetMock<INetworkInfoService>()
                .SetupGet(t => t.LanMacAddress).Returns(Convert.FromHexString("00:15:5d:02:09:2d".Replace(":", "")));
            mocker.GetMock<INetworkInfoService>()
                .SetupGet(t => t.WanMacAddress).Returns(Convert.FromHexString("00:15:5d:02:09:02".Replace(":", "")));
            mocker.GetMock<INetworkInfoService>()
                .SetupGet(t => t.WanNetmask).Returns(IPAddress.Parse("255.255.255.0"));

            mocker.GetMock<IConfigurationReader>()
                .Setup(t => t.LoadConfiguration())
                .Returns(new Configuration
                {
                    InformUrl = "http://192.168.2.12:8080/inform",
                    Key = "1e5385ae42f06ac27f768766f0e221ad",
                    Adopted = false
                });

            mocker.GetMock<IServiceProvider>()
                .Setup(t => t.GetService(typeof(ISystemInfoService)))
                .Returns(mocker.GetMock<ISystemInfoService>().Object);

            mocker.GetMock<IServiceProvider>()
                .Setup(t => t.GetService(typeof(INetworkInfoService)))
                .Returns(mocker.GetMock<INetworkInfoService>().Object);

            mocker.GetMock<IServiceProvider>()
                .Setup(t => t.GetService(typeof(IConfigurationReader)))
                .Returns(mocker.GetMock<IConfigurationReader>().Object);

            mocker.GetMock<IServiceProvider>()
                .Setup(t => t.GetService(typeof(IConfigurationWriter)))
                .Returns(mocker.GetMock<IConfigurationWriter>().Object);

            mocker.GetMock<IServiceProvider>()
                .Setup(t => t.GetService(typeof(IConnectRequest)))
                .Returns(mocker.GetMock<IConnectRequest>().Object);

            device = mocker.CreateInstance<UGW4Device>();
        }

        [Fact]
        public async Task GetInformMessage()
        {
            // Arrange

            // Act
            var message = await device.GetInformMessageAsync();

            // Assert
            Assert.NotNull(message);
        }
    }
}
