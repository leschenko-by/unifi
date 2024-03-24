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
            var key = Convert.FromHexString("441df6c0f401e12178a50c40232c2a49");
            var data = File.ReadAllBytes("request.bin");
            var decoded = decoder.Decode(data, key);
            var test = Encoding.UTF8.GetString(decoded);
            Assert.NotNull(test);
        }
    }
}
