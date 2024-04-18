using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Unifi.Gateway.Common.Models.SpeedTest;
using Unifi.Gateway.V1.Models;

namespace Unifi.Gateway.Common.Models
{
    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(Configuration))]
    [JsonSerializable(typeof(InformResponseMessage))]
    [JsonSerializable(typeof(SystemConfiguration), TypeInfoPropertyName = "SystemConfigurationV1")]
    [JsonSerializable(typeof(JsonObject))]
    [JsonSerializable(typeof(LocationModel))]
    [JsonSerializable(typeof(Server))]
    [JsonSerializable(typeof(List<Server>), TypeInfoPropertyName = "ListOfServer")]
    public partial class SourceGenerationContext : JsonSerializerContext
    {
    }
}
