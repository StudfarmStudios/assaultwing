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
        public int Length;
        public IPEndPoint EndPoint;

        public NetBuffer(byte[] buffer, IPEndPoint endPoint)
            : this(buffer, buffer.Length, endPoint)
        {
        }

        public NetBuffer(byte[] buffer, int length, IPEndPoint endPoint)
        {
            Buffer = buffer;
            Length = length;
            EndPoint = endPoint;
        }
    }
}
