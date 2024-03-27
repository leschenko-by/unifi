namespace Unifi.Gateway.Common.Interfaces
{
    public interface IConnectRequest
    {
        bool IsActive { get; set; }
        int Port { get; set; }
    }
}
