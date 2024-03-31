using Unifi.Gateway.Common.Interfaces;

namespace Unifi.Gateway.Common.Services
{
    public class ConnectRequest : IConnectRequest
    {
        private volatile int counter;

        public int Port { get; set; } = 23513;

        public void Activate()
        {
            Interlocked.Increment(ref counter);
        }

        public bool IsActive()
        {
            if (counter > 0)
            {
                Interlocked.Decrement(ref counter);
                return true;
            }

            return false;
        }
    }
}
