using Unifi.Gateway.Common.Enums;

namespace Unifi.Gateway.Common.Interfaces
{
    public interface IRequestEncoder
    {
        byte[] Encode(byte[] data, byte[] key, byte[] mac, CompressMode compress, EncryptMode encrypt);
    }
}
