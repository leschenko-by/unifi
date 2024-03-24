using System.Text.Json;
using Unifi.Gateway.Common.Interfaces;
using Unifi.Gateway.Json;

namespace Unifi.Gateway.Common.Services
{
    public class ConfigurationReader : IConfigurationReader
    {
        public AdoptOptions LoadConfiguration()
        {
            if (File.Exists("/etc/unifi/config.json"))
            {
                var json = File.ReadAllText("/etc/unifi/config.json");
                return JsonSerializer.Deserialize(json, SourceGenerationContext.Default.AdoptOptions)
                    ?? new AdoptOptions();
            }

            return new AdoptOptions();
        }
    }
}
