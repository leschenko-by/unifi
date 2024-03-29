
namespace Unifi.Gateway.Common.Interfaces
{
    public interface IUnifiProtocol
    {
        Task<byte[]> SendRequestAsync(string informUrl, byte[] key, byte[] data, CancellationToken token = default);
    }
}
