using System.Xml.Serialization;

namespace Unifi.SpeedTest.Models
{
    [XmlRoot("settings")]
    public class ServersList
    {
        [XmlArray("servers")]
        [XmlArrayItem("server")]
        public List<Server> Servers { get; set; } = [];

        public void CalculateDistances(Coordinate clientCoordinate)
        {
            foreach (var server in Servers)
            {
                server.Distance = clientCoordinate.GetDistanceTo(server.GeoCoordinate);
            }
        }
    }
}