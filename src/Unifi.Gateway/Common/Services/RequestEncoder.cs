using Snappy.Sharp;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using Unifi.Gateway.Common.Interfaces;

namespace Unifi.Gateway.Common.Services
{
    public class RequestEncoder : IRequestEncoder
    {
        public byte[] Encode(byte[] payload, byte[] key, byte[] mac, CompressMode compress, EncryptMode encrypt)
        {
            var iv = new byte[16];
            RandomNumberGenerator.Fill(iv);

            var compressFlags = compress switch
            {
                CompressMode.Zlib => 0x02,
                CompressMode.Snappy => 0x04,
                _ => 0x00,
            };

            var flags = (short)(compressFlags | (int)encrypt);

            var encoded = new MemoryStream();
            encoded.Write(Encoding.ASCII.GetBytes("TNBU"));
            encoded.Write(BitConverter.GetBytes(0));
            encoded.Write(mac);
            encoded.Write(BitConverter.GetBytes(flags).Reverse().ToArray());
            encoded.Write(iv);
            encoded.Write(BitConverter.GetBytes(1).Reverse().ToArray());

            payload = CompressData(payload, compress);

            switch (encrypt)
            {
                case EncryptMode.Gcm:
                    throw new NotImplementedException("dotnet AesGcm doesn't support 16 bytes nonces");

                    //encoded.Write(BitConverter.GetBytes(payload.Length + 16).Reverse().ToArray());
                    //using (var aes = new AesGcm(key, 16))
                    //{
                    //    // need convert 16 bytes to 12 bytes
                    //    // current implementation is wrong
                    //    // todo: read about GHASH
                    //    var nonce = iv.AsSpan()[..12].ToArray();
                    //    byte[] cipherText = new byte[payload.Length];
                    //    byte[] tag = new byte[16];
                    //    aes.Encrypt(nonce, payload, cipherText, tag, encoded.ToArray());
                    //    encoded.Write(cipherText);
                    //    encoded.Write(tag);
                    //}
                    //break;
                case EncryptMode.Cbc:
                    using (var aes = Aes.Create())
                    {
                        aes.Mode = CipherMode.CBC;
                        aes.Key = key;
                        aes.IV = iv;

                        var encryptor = aes.CreateEncryptor(key, iv);
                        var encrypted = encryptor.TransformFinalBlock(payload, 0, payload.Length);

                        encoded.Write(BitConverter.GetBytes(encrypted.Length).Reverse().ToArray());
                        encoded.Write(encrypted);
                    }
                    break;
                default:
                    encoded.Write(BitConverter.GetBytes(payload.Length).Reverse().ToArray());
                    encoded.Write(payload);
                    break;
            }

            return encoded.ToArray();
        }

        private static byte[] CompressData(byte[] data, CompressMode compress)
        {
            switch (compress)
            {
                case CompressMode.Zlib:
                    {
                        using var compressed = new MemoryStream();
                        using var compressor = new ZLibStream(compressed, CompressionMode.Compress);
                        compressor.Write(data);
                        compressor.Flush();
                        return compressed.ToArray();
                    }
                case CompressMode.Snappy:
                    {
                        var compressor = new SnappyCompressor();
                        int compressedSize = compressor.MaxCompressedLength(data.Length);
                        var compressed = new byte[compressedSize];
                        var size = compressor.Compress(data, 0, data.Length, compressed);
                        return compressed[..size];
                    }
                default:
                    return data;
            }
        }
    }
}
