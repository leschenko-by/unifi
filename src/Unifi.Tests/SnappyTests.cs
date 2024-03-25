using Snappy.Sharp;
using System.Text;

namespace Unifi.Tests
{
    public class SnappyTests
    {
        [Fact]
        public void Decompress()
        {
            var data = File.ReadAllBytes("snappy.bin");
            var payload = new SnappyDecompressor().Decompress(data, 0, data.Length);
            var test = Encoding.UTF8.GetString(payload);
            Assert.NotNull(test);
        }
    }
}
