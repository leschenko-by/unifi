namespace Unifi.Gateway.Common.Interfaces
{
    public interface IRequestDecoder
    {
        byte[] Decode(byte[] data, byte[] key);
    }
}
