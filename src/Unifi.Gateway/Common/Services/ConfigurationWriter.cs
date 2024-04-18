using System.Text.Json;
using Unifi.Gateway.Common.Interfaces;
using Unifi.Gateway.Common.Models;

namespace Unifi.Gateway.Common.Services
{
    public class ConfigurationWriter : IConfigurationWriter
    {
        public void SaveConfiguration(Configuration configuration)
        {
            var json = JsonSerializer.Serialize(configuration, SourceGenerationContext.Default.Configuration);
            Directory.CreateDirectory("/etc/unifi");
            File.WriteAllText("/etc/unifi/config.json", json);
        }
    }
}
