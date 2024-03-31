using Unifi.Gateway.Common.Interfaces;
using Unifi.Gateway.Common.Services;
using Unifi.Gateway.Devices;
using Unifi.Gateway.Models;

namespace Unifi.Gateway
{
    public static class ServiceRegister
    {
        public static void Register(IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<GeneralServiceOptions>(configuration.GetSection("DiscoveryService"));

            services.AddSingleton<IConnectRequest, ConnectRequest>();

            services.AddKeyedTransient<ISystemConfigurationService, SystemConfigurationV1Service>("v1");
            services.AddKeyedTransient<ISystemConfigurationService, SystemConfigurationV2Service>("v2");
            services.AddTransient<ISystemInfoService, SystemInfoService>();
            services.AddTransient<IRequestEncoder, RequestEncoder>();
            services.AddTransient<IRequestDecoder, RequestDecoder>();
            services.AddTransient<INetworkInfoService, NetworkInfoService>();
            services.AddTransient<IConfigurationReader, ConfigurationReader>();
            services.AddTransient<IConfigurationWriter, ConfigurationWriter>();
            services.AddTransient<IUnifiProtocol, UnifiProtocol>();
            services.AddTransient<IUfwService, UfwService>();
            services.AddKeyedTransient<IUnifiDevice, UGW3Device>("UGW3");
            services.AddKeyedTransient<IUnifiDevice, UGW4Device>("UGW4");
            services.AddKeyedTransient<IUnifiDevice, UXGDevice>("UXG");
        }
    }
}
