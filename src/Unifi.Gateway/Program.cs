using System.Text.Json;
using Unifi.Gateway.Json;
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
    var options = new AdoptOptions
    {
        InformUrl = args[1],
        Key = args[2]
    };
    var json = JsonSerializer.Serialize(options, SourceGenerationContext.Default.AdoptOptions);
    Directory.CreateDirectory("/etc/unifi");
    await File.WriteAllTextAsync("/etc/unifi/config.json", json);
}

static async Task RunAsync(string[] args)
{
    var builder = WebApplication.CreateSlimBuilder(args);
    builder.Services.Configure<DiscoveryServiceOptions>(builder.Configuration.GetSection("DiscoveryService"));
    builder.Services.AddHostedService<DiscoveryService>();
    builder.Services.AddHostedService<InformService>();
    builder.Services.AddSystemd();
    var app = builder.Build();
    app.MapGet("/", () => Results.Text("Ok"));
    await app.RunAsync();
}