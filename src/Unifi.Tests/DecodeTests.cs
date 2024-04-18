using System.Text;
using Unifi.Gateway.Common.Services;

namespace Unifi.Tests
{
    public class DecodeTests
    {
        private readonly UnifiDecoder decoder = new();

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
        public void DecodeRequestUS8()
        {
            var key = Convert.FromHexString("FA28E04A5F0BCD53542E404C6A3389E4");
            var data = File.ReadAllBytes(Path.Combine("dump", $"us-8.bin"));
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


        [Fact]
        public void AdoptTest()
        {
            var key = Convert.FromHexString("F407D794D75F85FC2D9B0A8146F601C1");
            for (var i = 1; i <= 5; i++)
            {
                var data = File.ReadAllBytes(Path.Combine("dump", $"r{i:00}-req.bin"));
                var request = decoder.Decode(data, key);
                File.WriteAllText(Path.Combine("dump", $"r{i:00}-req.json"), Encoding.UTF8.GetString(request));

                data = File.ReadAllBytes(Path.Combine("dump", $"r{i:00}-res.bin"));
                var response = decoder.Decode(data, key);
                File.WriteAllText(Path.Combine("dump", $"r{i:00}-res.json"), Encoding.UTF8.GetString(response));
            }

            //var data = File.ReadAllBytes("response.bin");
            //var decoded = decoder.Decode(data, key);
            //var test = Encoding.UTF8.GetString(decoded);
            //Assert.NotNull(test);
        }
    }
}
