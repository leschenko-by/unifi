using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using Snappy.Sharp;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using Unifi.Gateway.Common.Interfaces;

namespace Unifi.Gateway.Common.Services
{
    public class UnifiDecoder : IUnifiDecoder
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
                    var cipher = new GcmBlockCipher(new AesEngine());
                    var parameters = new AeadParameters(new KeyParameter(key), 128, iv, data[0..40]);
                    cipher.Init(false, parameters);

                    byte[] cipherText = payload;
                    var plainText = new byte[cipher.GetOutputSize(cipherText.Length)];
                    var len = cipher.ProcessBytes(cipherText, 0, cipherText.Length, plainText, 0);
                    cipher.DoFinal(plainText, len);

                    payload = plainText;
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
