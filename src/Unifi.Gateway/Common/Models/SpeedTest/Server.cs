using System.Text.Json.Serialization;

namespace Unifi.Gateway.Common.Models.SpeedTest
{
    public class Server
    {
        public string Sponsor { get; set; } = string.Empty;

        public string Url { get; set; } = string.Empty;

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        [JsonIgnore]
        private readonly Lazy<Coordinate> geoCoordinate;

        [JsonIgnore]
        internal Coordinate GeoCoordinate
        {
            get { return geoCoordinate.Value; }
        }

        [JsonIgnore]
        internal double Distance { get; set; }

        public Server()
        {
            geoCoordinate = new Lazy<Coordinate>(() => new Coordinate(Latitude, Longitude));
        }
    }
}