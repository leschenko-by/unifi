using Unifi.Gateway;

var builder = WebApplication.CreateSlimBuilder(args);
builder.Services.Configure<DiscoveryServiceOptions>(builder.Configuration.GetSection("DiscoveryService"));
builder.Services.AddHostedService<DiscoveryService>();
var app = builder.Build();
app.MapGet("/", () => Results.Text("Ok"));
app.Run();
