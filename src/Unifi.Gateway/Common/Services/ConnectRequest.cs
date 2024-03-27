using Unifi.Gateway.Common.Interfaces;

namespace Unifi.Gateway.Common.Services
{
    public class ConnectRequest : IConnectRequest
    {
        public bool IsActive { get; set; }
        public int Port { get; set; } = 23513;
    }
}
