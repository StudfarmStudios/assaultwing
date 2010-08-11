using System;
using System.Net;
using AW2.Helpers;

namespace AW2.Net
{
    public class AWEndPoint
    {
        public IPEndPoint UDPEndPoint { get; private set; }
        public IPEndPoint TCPEndPoint { get; private set; }

        public AWEndPoint(IPEndPoint udpEndPoint, int tcpPort)
        {
            UDPEndPoint = udpEndPoint;
            TCPEndPoint = new IPEndPoint(udpEndPoint.Address, tcpPort);
        }

        public static AWEndPoint Parse(string text)
        {
            int splitIndex = text.LastIndexOf(':');
            var udpEndPoint = MiscHelper.ParseIPEndPoint(text.Substring(0, splitIndex));
            int tcpPort = int.Parse(text.Substring(splitIndex + 1));
            return new AWEndPoint(udpEndPoint, tcpPort);
        }
    }
}
