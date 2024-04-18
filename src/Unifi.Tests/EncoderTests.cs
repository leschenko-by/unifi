using Moq.AutoMock;
using System.Text;
using Unifi.Gateway.Common.Enums;
using Unifi.Gateway.Common.Services;

namespace Unifi.Tests
{
    public class EncoderTests
    {
        private readonly AutoMocker mocker = new();
        private readonly UnifiEncoder encoder;
        private readonly UnifiDecoder decoder;

        public EncoderTests()
        {
            encoder = mocker.CreateInstance<UnifiEncoder>();
            decoder = mocker.CreateInstance<UnifiDecoder>();
        }

        [Fact]
        public void Decode_Cbc()
        {
            // Arrange
            var key = Convert.FromHexString("441df6c0f401e12178a50c40232c2a49");
            var mac = Convert.FromHexString("00:15:5d:02:09:2d".Replace(":", ""));

            var source = $$"""
                {
                    "cmd": "info",
                    "data": {
                        "version": "4.3.21.11357",
                        "platform": "UGW3",
                        "model": "UGW3",
                        "mac": "00:15:5d:02:09:2d",
                        "serial": "Q2LN-9B7B-9B7B-9B7B-9B7B",
                        "uptime": 123456,
                        "cfgversion": "9B7B9B7B9B7B9B7B9B7B9B7B9B7B9B7B",
                        "inform_ip": "
                    }
                }
                """;
            var data = Encoding.UTF8.GetBytes(source);

            var encoded = encoder.Encode(data, key, mac, CompressMode.Zlib, EncryptMode.Cbc);

            // Act
            var decoded = decoder.Decode(encoded, key);

            // Assert
            Assert.NotNull(decoded);
            var text = Encoding.UTF8.GetString(decoded);
            Assert.Equal(source, text);
        }

        [Fact]
        public void Decode_Gcm()
        {
            // Arrange
            var key = Convert.FromHexString("441df6c0f401e12178a50c40232c2a49");
            var mac = Convert.FromHexString("00:15:5d:02:09:2d".Replace(":", ""));

            var source = $$"""
                {
                    "cmd": "info",
                    "data": {
                        "version": "4.3.21.11357",
                        "platform": "UGW3",
                        "model": "UGW3",
                        "mac": "00:15:5d:02:09:2d",
                        "serial": "Q2LN-9B7B-9B7B-9B7B-9B7B",
                        "uptime": 123456,
                        "cfgversion": "9B7B9B7B9B7B9B7B9B7B9B7B9B7B9B7B",
                        "inform_ip": "
                    }
                }
                """;
            var data = Encoding.UTF8.GetBytes(source);

            var encoded = encoder.Encode(data, key, mac, CompressMode.Zlib, EncryptMode.Gcm);

            // Act
            var decoded = decoder.Decode(encoded, key);

            // Assert
            Assert.NotNull(decoded);
            var text = Encoding.UTF8.GetString(decoded);
            Assert.Equal(source, text);
        }

        [Fact]
        public void Decode_Cbc_Snappy()
        {
            // Arrange
            var key = Convert.FromHexString("441df6c0f401e12178a50c40232c2a49");
            var mac = Convert.FromHexString("00:15:5d:02:09:2d".Replace(":", ""));

            var source = $$"""
                {
                    "cmd": "info",
                    "data": {
                        "version": "4.3.21.11357",
                        "platform": "UGW3",
                        "model": "UGW3",
                        "mac": "00:15:5d:02:09:2d",
                        "serial": "Q2LN-9B7B-9B7B-9B7B-9B7B",
                        "uptime": 123456,
                        "cfgversion": "9B7B9B7B9B7B9B7B9B7B9B7B9B7B9B7B",
                        "inform_ip": "
                    }
                }
                """;
            var data = Encoding.UTF8.GetBytes(source);

            var encoded = encoder.Encode(data, key, mac, CompressMode.Snappy, EncryptMode.Cbc);

            // Act
            var decoded = decoder.Decode(encoded, key);

            // Assert
            Assert.NotNull(decoded);
            var text = Encoding.UTF8.GetString(decoded);
            Assert.Equal(source, text);
        }

        [Fact]
        public void Decode_Cbc_WithoutCompression()
        {
            // Arrange
            var key = Convert.FromHexString("441df6c0f401e12178a50c40232c2a49");
            var mac = Convert.FromHexString("00:15:5d:02:09:2d".Replace(":", ""));

            var source = $$"""
                {
                    "cmd": "info",
                    "data": {
                        "version": "4.3.21.11357",
                        "platform": "UGW3",
                        "model": "UGW3",
                        "mac": "00:15:5d:02:09:2d",
                        "serial": "Q2LN-9B7B-9B7B-9B7B-9B7B",
                        "uptime": 123456,
                        "cfgversion": "9B7B9B7B9B7B9B7B9B7B9B7B9B7B9B7B",
                        "inform_ip": "
                    }
                }
                """;
            var data = Encoding.UTF8.GetBytes(source);

            var encoded = encoder.Encode(data, key, mac, CompressMode.None, EncryptMode.Cbc);

            // Act
            var decoded = decoder.Decode(encoded, key);

            // Assert
            Assert.NotNull(decoded);
            var text = Encoding.UTF8.GetString(decoded);
            Assert.Equal(source, text);
        }

        [Fact]
        public void Decode_WithoutEncryptionAndCompression()
        {
            // Arrange
            var key = Convert.FromHexString("441df6c0f401e12178a50c40232c2a49");
            var mac = Convert.FromHexString("00:15:5d:02:09:2d".Replace(":", ""));

            var source = $$"""
                {
                    "cmd": "info",
                    "data": {
                        "version": "4.3.21.11357",
                        "platform": "UGW3",
                        "model": "UGW3",
                        "mac": "00:15:5d:02:09:2d",
                        "serial": "Q2LN-9B7B-9B7B-9B7B-9B7B",
                        "uptime": 123456,
                        "cfgversion": "9B7B9B7B9B7B9B7B9B7B9B7B9B7B9B7B",
                        "inform_ip": "
                    }
                }
                """;
            var data = Encoding.UTF8.GetBytes(source);

            var encoded = encoder.Encode(data, key, mac, CompressMode.None, EncryptMode.None);

            // Act
            var decoded = decoder.Decode(encoded, key);

            // Assert
            Assert.NotNull(decoded);
            var text = Encoding.UTF8.GetString(decoded);
            Assert.Equal(source, text);
        }

    }
}