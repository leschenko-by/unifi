namespace Unifi.Tests
{
    //public class UnifiGatewayDeviceTests
    //{
    //    private readonly AutoMocker mocker = new();
    //    private readonly UGW4Device device;

    //    public UnifiGatewayDeviceTests()
    //    {
    //        var options = Options.Create(new GeneralServiceOptions
    //        {
    //            DiscoveryPortId = 0,
    //        });
    //        mocker.Use(options);

    //        mocker.GetMock<IConfigurationReader>()
    //            .Setup(t => t.LoadConfiguration())
    //            .Returns(new Configuration
    //            {
    //                InformUrl = "http://192.168.2.12:8080/inform",
    //                Key = "1e5385ae42f06ac27f768766f0e221ad",
    //                Adopted = false
    //            });

    //        mocker.GetMock<IServiceProvider>()
    //            .Setup(t => t.GetService(typeof(ISystemInfoService)))
    //            .Returns(mocker.GetMock<ISystemInfoService>().Object);

    //        mocker.GetMock<IServiceProvider>()
    //            .Setup(t => t.GetService(typeof(INetworkInfoService)))
    //            .Returns(mocker.GetMock<INetworkInfoService>().Object);

    //        mocker.GetMock<INetworkInfoService>()
    //            .SetupGet(t => t.Interfaces)
    //            .Returns([new Mock<IEthernetInterface>().Object]);

    //        mocker.GetMock<IServiceProvider>()
    //            .Setup(t => t.GetService(typeof(IConfigurationReader)))
    //            .Returns(mocker.GetMock<IConfigurationReader>().Object);

    //        mocker.GetMock<IServiceProvider>()
    //            .Setup(t => t.GetService(typeof(IConfigurationWriter)))
    //            .Returns(mocker.GetMock<IConfigurationWriter>().Object);

    //        mocker.GetMock<IServiceProvider>()
    //            .Setup(t => t.GetService(typeof(IConnectRequest)))
    //            .Returns(mocker.GetMock<IConnectRequest>().Object);

    //        mocker.GetMock<IServiceProvider>()
    //            .Setup(t => t.GetService(typeof(IOptions<GeneralServiceOptions>)))
    //            .Returns(options);

    //        device = mocker.CreateInstance<UGW4Device>();
    //    }

    //    [Fact(Skip = "")]
    //    public async Task GetInformMessage()
    //    {
    //        // Arrange

    //        // Act
    //        var message = await device.GetInformMessageAsync();

    //        // Assert
    //        Assert.NotNull(message);
    //    }
    //}
}
