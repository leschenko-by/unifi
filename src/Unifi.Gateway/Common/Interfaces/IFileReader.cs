
namespace Unifi.Gateway.Common.Interfaces
{
    public interface IFileReader
    {
        Task<string> ReadAsync(string file);
    }
}
