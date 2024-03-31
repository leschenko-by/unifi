using System.Security.Cryptography;
using System.Text.Json;
using Unifi.Gateway;
using Unifi.Gateway.BackgroundServices;
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
    ServiceRegister.Register(builder.Services, builder.Configuration);

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