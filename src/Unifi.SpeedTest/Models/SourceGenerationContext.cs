using System.Text.Json.Serialization;
using Unifi.SpeedTest.Models;

namespace SpeedTest.Net.Models
{
    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(LocationModel))]
    internal partial class SourceGenerationContext : JsonSerializerContext
    {
    }
}
