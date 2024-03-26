using System.Text.RegularExpressions;

namespace Unifi.SpeedTest.Models
{
    public partial class Tools
    {
        [GeneratedRegex("<server url=\\\"(?<url>[^\\\"]+)\\\" lat=\\\"(?<lat>[^\\\"]+)\\\" lon=\\\"(?<lon>[^\\\"]+)\\\"")]
        public static partial Regex GetServerRegex();
    }
}
