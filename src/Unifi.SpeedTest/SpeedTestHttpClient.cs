using System.Globalization;
using System.Runtime.InteropServices;
using System.Web;
using System.Xml.Serialization;

namespace Unifi.SpeedTest
{
    internal class SpeedTestHttpClient : HttpClient
    {
        public int ConnectionLimit { get; set; }

        public SpeedTestHttpClient()
        {
            var frameworkInfo = RuntimeInformation.FrameworkDescription.Split();
            var frameworkName = $"{frameworkInfo[0]}{frameworkInfo[1]}";

            var osInfo = RuntimeInformation.OSDescription.Split();

            DefaultRequestHeaders.Add("Accept", "text/html, application/xhtml+xml, */*");
            DefaultRequestHeaders.Add("User-Agent", string.Join(" ",
            [
                "Mozilla/5.0",
                $"({osInfo[0]}-{osInfo[1]}; U; {RuntimeInformation.ProcessArchitecture}; en-us)",
                $"{frameworkName}/{frameworkInfo[1]}",
                "(KHTML, like Gecko)",
                $"SpeedTest.Net/1.0.0"
            ]));
        }

        public async Task<T> GetConfig<T>(string url)
        {
            var data = await GetStringAsync(AddTimeStamp(new Uri(url)));
            var xmlSerializer = new XmlSerializer(typeof(T));
            using var reader = new StringReader(data);
            return (T)xmlSerializer.Deserialize(reader);
        }

        private static Uri AddTimeStamp(Uri address)
        {
            var uriBuilder = new UriBuilder(address);
            var query = HttpUtility.ParseQueryString(uriBuilder.Query);
            query["x"] = DateTime.Now.ToFileTime().ToString(CultureInfo.InvariantCulture);
            uriBuilder.Query = query.ToString();
            return uriBuilder.Uri;
        }
    }
}
