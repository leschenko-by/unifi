namespace Unifi.Gateway.Common.Interfaces
{
    public interface IConnectRequest
    {
        int Port { get; set; }

        public void Activate();
        public bool IsActive();
    }
}
