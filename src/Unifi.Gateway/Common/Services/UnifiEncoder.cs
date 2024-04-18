using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using Snappy.Sharp;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using Unifi.Gateway.Common.Enums;
using Unifi.Gateway.Common.Interfaces;

namespace Unifi.Gateway.Common.Services
{
    public class UnifiEncoder : IUnifiEncoder
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

            return EncryptData(encrypt, payload, key, iv, encoded);
        }

        private static byte[] EncryptData(EncryptMode encrypt, byte[] payload, byte[] key, byte[] iv, MemoryStream data)
        {
            switch (encrypt)
            {
                case EncryptMode.Gcm:
                    data.Write(BitConverter.GetBytes(payload.Length + 16).Reverse().ToArray());

                    var cipher = new GcmBlockCipher(new AesEngine());
                    var parameters = new AeadParameters(new KeyParameter(key), 128, iv, data.ToArray());
                    cipher.Init(true, parameters);

                    var cipherText = new byte[cipher.GetOutputSize(payload.Length)];
                    var len = cipher.ProcessBytes(payload, 0, payload.Length, cipherText, 0);
                    cipher.DoFinal(cipherText, len);

                    data.Write(cipherText);
                    break;
                case EncryptMode.Cbc:
                    using (var aes = Aes.Create())
                    {
                        aes.Mode = CipherMode.CBC;
                        aes.Key = key;
                        aes.IV = iv;

                        var encryptor = aes.CreateEncryptor(key, iv);
                        var encrypted = encryptor.TransformFinalBlock(payload, 0, payload.Length);

                        data.Write(BitConverter.GetBytes(encrypted.Length).Reverse().ToArray());
                        data.Write(encrypted);
                    }
                    break;
                default:
                    data.Write(BitConverter.GetBytes(payload.Length).Reverse().ToArray());
                    data.Write(payload);
                    break;
            }

            return data.ToArray();
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
