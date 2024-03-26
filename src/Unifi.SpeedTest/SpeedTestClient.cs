using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using SpeedTest.Net.Models;
using Unifi.SpeedTest.Models;

namespace Unifi.SpeedTest
{
    public class SpeedTestClient : ISpeedTestClient
    {
        private static readonly int[] DownloadSizes = [350, 750, 1500, 3000];
        private const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const int MaxUploadSize = 4; // 400 KB

        #region ISpeedTestClient

        /// <inheritdoc />
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<Server> GetServerAsync()
        {
            var client = new HttpClient();
            var loc = JsonSerializer.Deserialize(
                await client.GetStringAsync("https://ipinfo.io/json"),
                SourceGenerationContext.Default.LocationModel);
            var coordinate = new Coordinate(loc.Latitude, loc.Longitude);

            var text = await client.GetStringAsync("http://www.speedtest.net/speedtest-servers-static.php");
            var lines = text.Split('\n');
            var rx = Tools.GetServerRegex();
            var servers = lines
                .Select(line => rx.Match(line))
                .Where(t => t.Success)
                .Select(t => new Server
                {
                    Url = t.Groups["url"].Value,
                    Latitude = double.Parse(t.Groups["lat"].Value, CultureInfo.InvariantCulture),
                    Longitude = double.Parse(t.Groups["lon"].Value, CultureInfo.InvariantCulture),
                    Sponsor = t.Groups["sponsor"].Value
                })
                .ToList();

            var config = new ServersList(servers);
            config.CalculateDistances(coordinate);
            return config.Servers.OrderBy(s => s.Distance).First();
        }

        /// <inheritdoc />
        public async Task<int> TestServerLatencyAsync(Server server, int retryCount = 3)
        {
            var latencyUri = CreateTestUrl(server, "latency.txt");
            var timer = new Stopwatch();

            using var client = new SpeedTestHttpClient();

            for (var i = 0; i < retryCount; i++)
            {
                string testString;
                try
                {
                    timer.Start();
                    testString = await client.GetStringAsync(latencyUri).ConfigureAwait(false);
                }
                catch (WebException)
                {
                    continue;
                }
                finally
                {
                    timer.Stop();
                }

                if (!testString.StartsWith("test=test"))
                {
                    throw new InvalidOperationException("Server returned incorrect test string for latency.txt");
                }
            }

            return (int)timer.ElapsedMilliseconds / retryCount;
        }

        /// <inheritdoc />
        public async Task<double> TestDownloadSpeedAsync(Server server, int simultaneousDownloads = 2, int retryCount = 2)
        {
            var testData = GenerateDownloadUrls(server, retryCount);

            return await TestSpeedAsync(testData, async (client, url) =>
            {
                var data = await client.GetByteArrayAsync(url).ConfigureAwait(false);
                return data.Length;
            }, simultaneousDownloads);
        }

        /// <inheritdoc />
        public async Task<double> TestUploadSpeedAsync(Server server, int simultaneousUploads = 2, int retryCount = 2)
        {
            var testData = GenerateUploadData(retryCount);
            return await TestSpeedAsync(testData, async (client, uploadData) =>
            {
                await client.PostAsync(server.Url, new StringContent(uploadData));
                return uploadData.Length;
            }, simultaneousUploads);
        }

        #endregion

        #region Helpers

        private static async Task<double> TestSpeedAsync<T>(IEnumerable<T> testData, Func<HttpClient, T, Task<int>> doWork, int concurrencyCount = 2)
        {
            var timer = new Stopwatch();
            var throttler = new SemaphoreSlim(concurrencyCount);

            timer.Start();
            var downloadTasks = testData.Select(async data =>
            {
                await throttler.WaitAsync().ConfigureAwait(false);
                var client = new SpeedTestHttpClient();
                try
                {
                    var size = await doWork(client, data).ConfigureAwait(false);
                    return size;
                }
                finally
                {
                    client.Dispose();
                    throttler.Release();
                }
            }).ToArray();

            await Task.WhenAll(downloadTasks);
            timer.Stop();

            double totalSize = downloadTasks.Sum(task => task.Result);
            return totalSize * 8 / 1024 / ((double)timer.ElapsedMilliseconds / 1000);
        }

        private static IEnumerable<string> GenerateUploadData(int retryCount)
        {
            var random = new Random();
            var result = new List<string>();

            for (var sizeCounter = 1; sizeCounter < MaxUploadSize + 1; sizeCounter++)
            {
                var size = sizeCounter * 200 * 1024;
                var builder = new StringBuilder(size);

                builder.AppendFormat("content{0}=", sizeCounter);

                for (var i = 0; i < size; ++i)
                {
                    builder.Append(Chars[random.Next(Chars.Length)]);
                }

                for (var i = 0; i < retryCount; i++)
                {
                    result.Add(builder.ToString());
                }
            }

            return result;
        }

        private static string CreateTestUrl(Server server, string file)
        {
            return new Uri(new Uri(server.Url), ".").OriginalString + file;
        }

        private static IEnumerable<string> GenerateDownloadUrls(Server server, int retryCount)
        {
            var downloadUriBase = CreateTestUrl(server, "random{0}x{0}.jpg?r={1}");
            foreach (var downloadSize in DownloadSizes)
            {
                for (var i = 0; i < retryCount; i++)
                {
                    yield return string.Format(downloadUriBase, downloadSize, i);
                }
            }
        }

        #endregion
    }
}
