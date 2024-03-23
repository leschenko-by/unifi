
namespace Unifi.Gateway.Common.Interfaces
{
    public interface IUnifiDevice
    {
        byte[] MacAddress { get; }
        string InformUrl { get; }
        byte[] Key { get; }

        string GetInformMessage();
        Task ReloadConfigsAsync();
        Task UpdateAsync(string json);
    }
}
