using System.Text.Json.Serialization;
using Unifi.Gateway.Models;

namespace Unifi.Gateway.Json
{
    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(Configuration))]
    [JsonSerializable(typeof(InformResponseMessage))]
    [JsonSerializable(typeof(SystemConfiguration))]
    [JsonSerializable(typeof(UnifiConfiguration))]
    internal partial class SourceGenerationContext : JsonSerializerContext
    {
    }
}
