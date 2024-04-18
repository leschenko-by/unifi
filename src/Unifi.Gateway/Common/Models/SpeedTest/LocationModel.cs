using System.Globalization;
using System.Text.Json.Serialization;

namespace Unifi.Gateway.Common.Models.SpeedTest
{
    public partial class LocationModel
    {
        [JsonPropertyName("ip")]
        public string Ip { get; set; } = string.Empty;

        [JsonPropertyName("hostname")]
        public string Hostname { get; set; } = string.Empty;

        [JsonPropertyName("city")]
        public string City { get; set; } = string.Empty;

        [JsonPropertyName("region")]
        public string Region { get; set; } = string.Empty;

        [JsonPropertyName("country")]
        public string Country { get; set; } = string.Empty;

        [JsonPropertyName("loc")]
        public string Loc { get; set; } = string.Empty;

        [JsonPropertyName("org")]
        public string Org { get; set; } = string.Empty;

        public double Latitude => double.Parse(Loc?.Split(',')?.FirstOrDefault()?.Trim() ?? "0", CultureInfo.InvariantCulture);
        public double Longitude => double.Parse(Loc?.Split(',')?.LastOrDefault()?.Trim() ?? "0", CultureInfo.InvariantCulture);
    }
}