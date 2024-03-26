using System.Text.RegularExpressions;

namespace Unifi.SpeedTest.Models
{
    public partial class Tools
    {
        [GeneratedRegex("<server url=\\\"(?<url>[^\\\"]+)\\\" lat=\\\"(?<lat>[^\\\"]+)\\\" lon=\\\"(?<lon>[^\\\"]+)\\\" .* sponsor=\\\"(?<sponsor>[^\\\"]+)\\\"")]
        public static partial Regex GetServerRegex();
    }
}
