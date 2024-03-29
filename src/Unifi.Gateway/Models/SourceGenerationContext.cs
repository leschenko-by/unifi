using System.Text.Json.Serialization;
using Unifi.Gateway.Models;

namespace Unifi.Gateway.Json
{
    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(Configuration))]
    [JsonSerializable(typeof(InformResponseMessage))]
    [JsonSerializable(typeof(SystemConfiguration))]
    internal partial class SourceGenerationContext : JsonSerializerContext
    {
    }
}
