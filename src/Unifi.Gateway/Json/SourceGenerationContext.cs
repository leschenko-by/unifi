using System.Text.Json.Serialization;

namespace Unifi.Gateway.Json
{
    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(Configuration))]
    [JsonSerializable(typeof(ResponseData))]
    internal partial class SourceGenerationContext : JsonSerializerContext
    {
    }
}
