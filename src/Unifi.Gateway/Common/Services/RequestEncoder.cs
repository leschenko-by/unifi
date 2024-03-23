using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using Unifi.Gateway.Common.Interfaces;

namespace Unifi.Gateway.Common.Services
{
    public class RequestEncoder : IRequestEncoder
    {
        public byte[] Encode(byte[] payload, byte[] key, byte[] mac, bool compress, EncryptMode encrypt)
        {
            var iv = new byte[16];
            RandomNumberGenerator.Fill(iv);

            var flags = (short)((compress ? 0x02 : 0x00) | (int)encrypt);

            var encoded = new MemoryStream();
            encoded.Write(Encoding.ASCII.GetBytes("TNBU"));
            encoded.Write(new byte[4]);
            encoded.Write(mac);
            encoded.Write(BitConverter.GetBytes(flags));
            encoded.Write(iv);
            encoded.Write(BitConverter.GetBytes(1));

            if (compress)
            {
                payload = CompressData(payload);
            }

            switch (encrypt)
            {
                case EncryptMode.Gcm:
                    encoded.Write(BitConverter.GetBytes(payload.Length + 16));
                    using (var aes = new AesGcm(key, AesGcm.TagByteSizes.MaxSize))
                    {
                        var nonce = iv.AsSpan()[..12];
                        byte[] cipherText = new byte[payload.Length];
                        byte[] tag = new byte[AesGcm.TagByteSizes.MaxSize];

                        aes.Encrypt(nonce, payload, cipherText, tag, encoded.ToArray());

                        encoded.Write(cipherText);
                        encoded.Write(tag);
                    }
                    break;
                case EncryptMode.Cbc:
                    using (var aes = Aes.Create())
                    {
                        aes.Mode = CipherMode.CBC;
                        aes.Key = key;
                        aes.IV = iv;

                        var encryptor = aes.CreateEncryptor(key, iv);
                        var encrypted = encryptor.TransformFinalBlock(payload, 0, payload.Length);

                        encoded.Write(BitConverter.GetBytes(encrypted.Length));
                        encoded.Write(encrypted);
                    }
                    break;
                default:
                    encoded.Write(BitConverter.GetBytes(payload.Length));
                    encoded.Write(payload);
                    break;
            }

            return encoded.ToArray();
        }

        private static byte[] CompressData(byte[] data)
        {
            using var compressed = new MemoryStream();
            using var compressor = new ZLibStream(compressed, CompressionMode.Compress);
            compressor.Write(data);
            compressor.Flush();
            return compressed.ToArray();
        }
    }
}
