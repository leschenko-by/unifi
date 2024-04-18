namespace Unifi.Gateway.Common.Interfaces
{
    public interface IUnifiDecoder
    {
        byte[] Decode(byte[] data, byte[] key);
    }
}
