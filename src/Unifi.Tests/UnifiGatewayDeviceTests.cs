using Microsoft.Extensions.Options;
using Moq.AutoMock;
using System.Net;
using Unifi.Gateway.Common.Devices;
using Unifi.Gateway.Common.Interfaces;
using Unifi.Gateway.Json;
using Unifi.Gateway.Options;

namespace Unifi.Tests
{
    public class UnifiGatewayDeviceTests
    {
        private readonly AutoMocker mocker = new();
        private readonly UnifiGatewayDevice device;

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

            mocker.GetMock<IConfigurationReader>()
                .Setup(t => t.LoadConfiguration())
                .Returns(new Configuration
                {
                    InformUrl = "http://192.168.2.12:8080/inform",
                    Key = "1e5385ae42f06ac27f768766f0e221ad",
                    Adopted = false
                });

            device = mocker.CreateInstance<UnifiGatewayDevice>();
        }

        [Fact]
        public void GetInformMessage()
        {
            // Arrange

            // Act
            var message = device.GetInformMessage();

            // Assert
            Assert.NotNull(message);
        }
    }
}
