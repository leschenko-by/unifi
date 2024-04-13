using System.Security.Cryptography;
using System.Text.Json;
using Unifi.Gateway.BackgroundServices;
using Unifi.Gateway.Common.Interfaces;
using Unifi.Gateway.Common.Services;
using Unifi.Gateway.Devices;
using Unifi.Gateway.Models;

if (args.Length > 0)
{
    switch (args[0])
    {
        case "set-adopt":
            await SetAdopt(args);
            return;
        default:
            Console.WriteLine("Unknown command");
            return;
    }
}

await RunAsync(args);

static async Task SetAdopt(string[] args)
{
    if (args.Length < 3)
    {
        Console.WriteLine("Usage: set-adopt http://controller-ip-address:8080/inform encryptionkey");
    }

    var fingerprint = new byte[20];
    RandomNumberGenerator.Fill(fingerprint);

    var serviceOptions = new GeneralServiceOptions();
    var config = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .Build();
    config.GetSection("DiscoveryService").Bind(serviceOptions);

    var options = new Configuration
    {
        InformUrl = args[1],
        Key = args[2],
        Adopted = false,
        Fingerprint = string.Join(":", fingerprint.Select(t => t.ToString("x2"))),
        Firmware = serviceOptions.Firmware,
    };
    var json = JsonSerializer.Serialize(options, SourceGenerationContext.Default.Configuration);
    Directory.CreateDirectory("/etc/unifi");
    await File.WriteAllTextAsync("/etc/unifi/config.json", json);
}

static async Task RunAsync(string[] args)
{
    var builder = WebApplication.CreateSlimBuilder(args);
    builder.Logging.AddConsole();
    builder.Services.AddHttpClient();
    builder.Services.AddSystemd();
    builder.Services.Configure<GeneralServiceOptions>(builder.Configuration.GetSection("DiscoveryService"));
    builder.Services.AddSingleton<IConnectRequest, ConnectRequest>();
    builder.Services.AddKeyedTransient<ISystemConfigurationService, SystemConfigurationV1Service>("v1");
    builder.Services.AddKeyedTransient<ISystemConfigurationService, SystemConfigurationV2Service>("v2");
    builder.Services.AddTransient<IFirewallBuilderIPv4, FirewallBuilderIPv4>();
    builder.Services.AddTransient<IFirewallBuilderIPv6, FirewallBuilderIPv6>();
    builder.Services.AddTransient<ISystemInfoService, SystemInfoService>();
    builder.Services.AddTransient<IRequestEncoder, RequestEncoder>();
    builder.Services.AddTransient<IRequestDecoder, RequestDecoder>();
    builder.Services.AddTransient<INetworkInfoService, NetworkInfoService>();
    builder.Services.AddTransient<IConfigurationReader, ConfigurationReader>();
    builder.Services.AddTransient<IConfigurationWriter, ConfigurationWriter>();
    builder.Services.AddTransient<IUnifiProtocol, UnifiProtocol>();
    builder.Services.AddTransient<IFirewallService, FirewallService>();
    builder.Services.AddTransient<IFileReader, FileReader>();
    builder.Services.AddKeyedTransient<IUnifiDevice, UGW3Device>("UGW3");
    builder.Services.AddKeyedTransient<IUnifiDevice, UGW4Device>("UGW4");
    builder.Services.AddKeyedTransient<IUnifiDevice, UXGDevice>("UXG");

    builder.Services.AddHostedService(provider =>
    {
        string[] devices = ["UXG", "UGW3", "UGW4"];

        var configuration = provider.GetRequiredService<IConfiguration>();
        var deviceName = configuration["Device"] ?? "";
        if (!devices.Contains(deviceName))
        {
            deviceName = "UXG";
        }

        return new UnifiDeviceService(deviceName, provider);
    });
    builder.Services.AddHostedService<ConnectRequestService>();

    var app = builder.Build();
    app.MapGet("/", () => Results.Text("Ok"));
    await app.RunAsync();
}