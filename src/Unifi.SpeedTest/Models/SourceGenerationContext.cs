using System.Text.Json.Serialization;
using Unifi.SpeedTest.Models;

namespace SpeedTest.Net.Models
{
    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(LocationModel))]
    [JsonSerializable(typeof(Server))]
    [JsonSerializable(typeof(List<Server>), TypeInfoPropertyName = "ListOfServer")]
    internal partial class SourceGenerationContext : JsonSerializerContext
    {
    }
}
