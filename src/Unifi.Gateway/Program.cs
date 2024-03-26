using System.Net.Mail;
using System.Security.Cryptography;
using System.Text.Json;
using Unifi.Gateway.Common.Devices;
using Unifi.Gateway.Common.Interfaces;
using Unifi.Gateway.Common.Services;
using Unifi.Gateway.Json;
using Unifi.Gateway.Options;
using Unifi.Gateway.Services;

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
    builder.Services.Configure<GeneralServiceOptions>(builder.Configuration.GetSection("DiscoveryService"));
    builder.Services.AddTransient<ISystemInfoService, SystemInfoService>();
    builder.Services.AddTransient<IRequestEncoder, RequestEncoder>();
    builder.Services.AddTransient<IRequestDecoder, RequestDecoder>();
    builder.Services.AddTransient<INetworkInfoService, NetworkInfoService>();
    builder.Services.AddTransient<IConfigurationReader, ConfigurationReader>();
    builder.Services.AddTransient<IConfigurationWriter, ConfigurationWriter>();
    builder.Services.AddTransient<IUnifiDevice, UnifiGatewayDevice>();
    builder.Services.AddHostedService<DiscoveryService>();
    builder.Services.AddHostedService<InformService>();
    builder.Services.AddHttpClient("inform", client =>
    {
        client.DefaultRequestHeaders.Add("User-Agent", "AirControl Agent v1.0");
    });
    builder.Services.AddSystemd();
    var app = builder.Build();
    app.MapGet("/", () => Results.Text("Ok"));
    await app.RunAsync();
}