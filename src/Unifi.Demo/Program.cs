using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Text.Json;
using Unifi.Gateway;
using Unifi.Gateway.Models;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        ServiceRegister.Register(services, hostContext.Configuration);
    })
    .Build();

using var scope = host.Services.CreateScope();
var options = scope.ServiceProvider.GetRequiredService<IOptions<GeneralServiceOptions>>().Value;

var client = new HttpClient();
client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse("Basic " + options.NTopAuth);

var json = await client.GetStringAsync(options.NTopUri);
var response = JsonSerializer.Deserialize(json, SourceGenerationContext.Default.NTopResponse);

if (response != null)
{
    var hosts = response.Hosts.Where(h => !string.IsNullOrEmpty(h.Router)).ToList();

    var query =
        from h in hosts
        group h by h.MacAddress into g
        select new
        {
            MacAddress = g.Key,
            IP = g.Where(t=>t.IpVersion == 4).Select(t=>t.IpAddress).FirstOrDefault(),
            Duration = g.Max(t => t.Duration),
            BytesSent = g.Sum(t => t.BytesSent),
            BytesReceived = g.Sum(t => t.BytesReceived),
            PacketsSent = g.Sum(t => t.PacketsSent),
            PacketsReceived = g.Sum(t => t.PacketsReceived),           
        };

    var groups = query.ToList();
    Console.WriteLine(groups.Count);
}

Console.WriteLine();