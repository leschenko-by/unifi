using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Unifi.Gateway.Models.NTop;

namespace Unifi.Gateway.Models
{
    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(NTopResponse))]
    [JsonSerializable(typeof(Configuration))]
    [JsonSerializable(typeof(InformResponseMessage))]
    [JsonSerializable(typeof(SystemConfiguration))]
    [JsonSerializable(typeof(JsonObject))]
    public partial class SourceGenerationContext : JsonSerializerContext
    {
    }
}
