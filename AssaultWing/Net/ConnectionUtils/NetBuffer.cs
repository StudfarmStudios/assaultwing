using System;
using System.Net;

namespace AW2.Net.ConnectionUtils
{
    /// <summary>
    /// Data buffer with end point information.
    /// </summary>
    public class NetBuffer
    {
        public byte[] Buffer;
        public IPEndPoint EndPoint;

        public NetBuffer(byte[] buffer, IPEndPoint endPoint)
        {
            Buffer = buffer;
            EndPoint = endPoint;
        }
    }
}
