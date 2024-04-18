using System.Text.Json;
using Unifi.Gateway.Common.Interfaces;
using Unifi.Gateway.Common.Models;

namespace Unifi.Gateway.Common.Services
{
    public class ConfigurationReader : IConfigurationReader
    {
        public Configuration LoadConfiguration()
        {
            if (File.Exists("/etc/unifi/config.json"))
            {
                var json = File.ReadAllText("/etc/unifi/config.json");
                return JsonSerializer.Deserialize(json, SourceGenerationContext.Default.Configuration)
                    ?? new Configuration();
            }

            return new Configuration();
        }
    }
}
