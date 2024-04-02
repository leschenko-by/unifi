using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Unifi.Gateway.Models.V1;

namespace Unifi.Gateway.Models
{
    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(Configuration))]
    [JsonSerializable(typeof(InformResponseMessage))]
    [JsonSerializable(typeof(SystemConfiguration), TypeInfoPropertyName = "SystemConfigurationV1")]
    [JsonSerializable(typeof(JsonObject))]
    public partial class SourceGenerationContext : JsonSerializerContext
    {
    }
}
