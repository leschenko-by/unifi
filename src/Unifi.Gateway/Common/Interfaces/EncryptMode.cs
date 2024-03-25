namespace Unifi.Gateway.Common.Interfaces
{
    public enum EncryptMode
    {
        None = 0x00,
        Cbc = 0x01,
        Gcm = 0x09,
    }

    public enum CompressMode
    {
        None,
        Zlib,
        Snappy,
    }
}
