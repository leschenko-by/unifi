using System.Text;
using Unifi.Gateway.Common.Services;

namespace Unifi.Tests
{
    public class DecodeTests
    {
        private readonly RequestDecoder decoder = new();

        [Fact]
        public void DecodeRequest()
        {
            var key = Convert.FromHexString("2f6f1d244fe16242e4610476db608875");
            var data = File.ReadAllBytes("request.bin");
            var decoded = decoder.Decode(data, key);
            var test = Encoding.UTF8.GetString(decoded);
            Assert.NotNull(test);
        }

        [Fact]
        public void DecodeResponse()
        {
            var key = Convert.FromHexString("2f6f1d244fe16242e4610476db608875");
            var data = File.ReadAllBytes("response.bin");
            var decoded = decoder.Decode(data, key);
            var test = Encoding.UTF8.GetString(decoded);
            Assert.NotNull(test);
        }
    }
}
