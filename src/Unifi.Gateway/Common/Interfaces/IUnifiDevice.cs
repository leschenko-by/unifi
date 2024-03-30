namespace Unifi.Gateway.Common.Interfaces
{
    public interface IUnifiDevice
    {
        string InformUrl { get; }
        byte[] Key { get; }
        TimeSpan Interval { get; set; }

        Task<string> GetInformMessageAsync();
        void LoadConfigration();
        Task ParseResponseAsync(string json);
        void SaveConfigration();
        Task SendDiscoveryAsync(int broadcastIndex);
    }
}
