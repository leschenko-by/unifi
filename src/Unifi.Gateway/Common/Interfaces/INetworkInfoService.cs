namespace Unifi.Gateway.Common.Interfaces
{
    public interface INetworkInfoService
    {
        public IReadOnlyList<IEthernetInterface?> Interfaces { get; }
    }
}
