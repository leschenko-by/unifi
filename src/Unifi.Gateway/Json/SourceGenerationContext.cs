using System.Text.Json.Serialization;

namespace Unifi.Gateway.Json
{
    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(AdoptOptions))]
    internal partial class SourceGenerationContext : JsonSerializerContext
    {
    }
}
