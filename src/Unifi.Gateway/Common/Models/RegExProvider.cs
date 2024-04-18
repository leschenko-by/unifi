using System.Text.RegularExpressions;

namespace Unifi.Gateway.Common.Models
{
    public partial class RegExProvider
    {
        [GeneratedRegex(@"(?<ip>\d+\.\d+\.\d+\.\d+)\s+\w+\s+\w+\s+(?<mac>[\w:]+)\s+\*\s+(?<nic>\w+)")]
        public static partial Regex GetArpRegEx();
    }
}
