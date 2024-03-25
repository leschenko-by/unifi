using Snappy.Sharp;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using Unifi.Gateway.Common.Interfaces;

namespace Unifi.Gateway.Common.Services
{
    public class RequestDecoder : IRequestDecoder
    {
        public byte[] Decode(byte[] data, byte[] key)
        {
            var signature = Encoding.ASCII.GetString(data[0..4]);
            if (signature != "TNBU")
            {
                throw new InvalidDataException("Invalid signature");
            }

            var flags = BitConverter.ToInt16(data[14..16].Reverse().ToArray());

            var dataLength = BitConverter.ToInt32(data[36..40].Reverse().ToArray());
            var payload = data[40..(40 + dataLength)];

            if ((flags & 0x01) != 0)
            {
                var iv = data[16..32];
                if ((flags & 0x08) != 0) // GCM
                {
                    throw new NotImplementedException("dotnet AesGcm doesn't support 16 bytes nonces");
                    //using var aes = new AesGcm(key, 16);
                    //var nonce = iv;
                    //byte[] cipherText = payload[0..^16];
                    //byte[] tag = payload[^16..];
                    //var decoded = new byte[cipherText.Length];
                    //aes.Decrypt(nonce, cipherText, tag, decoded, data[0..40]);
                    //payload = decoded;
                }
                else // CBC
                {
                    using var aes = Aes.Create();
                    aes.Mode = CipherMode.CBC;
                    aes.Key = key;
                    aes.IV = iv;

                    var decryptor = aes.CreateDecryptor(key, iv);
                    var decoded = decryptor.TransformFinalBlock(payload, 0, payload.Length);

                    payload = decoded;
                }
            }

            if ((flags & 0x02) != 0) // Zlib
            {
                payload = DecompressZLibData(payload);
            }
            else if ((flags & 0x04) != 0) // Snappy
            {
                payload = DecompressSnappyData(payload);
            }

            return payload;
        }

        private static byte[] DecompressSnappyData(byte[] payload)
        {
            return new SnappyDecompressor().Decompress(payload, 0, payload.Length);
        }

        private static byte[] DecompressZLibData(byte[] data)
        {
            using var input = new MemoryStream(data);
            using var output = new MemoryStream();
            using var zlib = new ZLibStream(input, CompressionMode.Decompress);
            zlib.CopyTo(output);
            return output.ToArray();
        }
    }
}
