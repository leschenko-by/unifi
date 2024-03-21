namespace Unifi.Gateway
{
    public class UnifyDatagramBuilder
    {
        private byte[] datagram = [];

        public void Add(byte type, byte[] data)
        {
            var length = data.Length;
            datagram = [.. datagram, type, (byte)(length >> 8), (byte)(length & 255), .. data];
        }

        public byte[] Build()
        {
            var length = datagram.Length;
            return [2, 6, (byte)(length >> 8), (byte)(length & 255), ..datagram];
        }
    }
}
