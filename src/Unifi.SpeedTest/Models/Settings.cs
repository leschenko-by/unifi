using SpeedTest.Net.Models;
using System.Text.Json;
using System.Xml.Serialization;

namespace Unifi.SpeedTest.Models
{
    [XmlRoot("settings")]
    public class Settings
    {
        [XmlElement("client")]
        public Client Client { get; set; }

        [XmlElement("times")]
        public Times Times { get; set; }

        [XmlElement("download")]
        public Download Download { get; set; }

        [XmlElement("upload")]
        public Upload Upload { get; set; }

        [XmlElement("server-config")]
        public ServerConfig ServerConfig { get; set; }

        public List<Server> Servers { get; set; }

        public Settings()
        {
            Servers = new List<Server>();
        }

        public async Task<Server> GetServer()
        {
            try
            {
                using var client = new HttpClient();
                var loc = JsonSerializer.Deserialize(
                    await client.GetStringAsync("https://ipinfo.io/json"),
                    SourceGenerationContext.Default.LocationModel);

                var coordinate = new Coordinate(loc.Latitude, loc.Longitude);
                return Servers.OrderBy(s => s.GeoCoordinate.GetDistanceTo(coordinate)).First();
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to get Server based on the callee location", ex);
            }
        }

    }
}