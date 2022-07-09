using System.Net;
using AW2.Helpers;
using Steamworks;

namespace AW2.Net
{
    public abstract class AWEndPoint {
        public static AWEndPoint Parse(string text)
        {
            if (text.StartsWith("raw:")) {
                return AWEndPointRaw.ParseRaw(text.Substring(4)); // raw:host:udpport:tcpport
            } else {
                return new AWEndPointSteam(text); // ip:127.0.0.1:1234 steamid:12345671234512345
            }
        }
    }

    /// <summary>
    /// A subtype of AW Endpoint of the format host:udpport:tcpport
    /// </summary>
    public class AWEndPointRaw : AWEndPoint
    {
        public IPEndPoint UDPEndPoint { get; private set; }
        public IPEndPoint TCPEndPoint { get; private set; }

        public AWEndPointRaw(IPEndPoint udpEndPoint, int tcpPort)
        {
            UDPEndPoint = udpEndPoint;
            TCPEndPoint = new IPEndPoint(udpEndPoint.Address, tcpPort);
        }

        public static AWEndPointRaw ParseRaw(string text)
        {
            int splitIndex = text.LastIndexOf(':');
            var udpEndPoint = MiscHelper.ParseIPEndPoint(text.Substring(0, splitIndex));
            int tcpPort = int.Parse(text.Substring(splitIndex + 1));
            return new AWEndPointRaw(udpEndPoint, tcpPort);
        }

        public override string ToString()
        {
            return string.Format("{0}:{1}", UDPEndPoint, TCPEndPoint.Port);
        }
    }

    public class AWEndPointSteam : AWEndPoint 
    {
        private SteamNetworkingIdentity _SteamNetworkingIdentity;

        public SteamNetworkingIdentity SteamNetworkingIdentity { get {return _SteamNetworkingIdentity;} }

        public AWEndPointSteam(string parsed) {
            if (!_SteamNetworkingIdentity.ParseString(parsed)) {
                throw new ArgumentException($"Unknown Steam Networking ID {parsed}. Example: ip:127.0.0.1:1234");
            }
        }

        public override string ToString()
        {
            // Example: ip:10.10.10.10:123
            SteamNetworkingIdentity localTemp = _SteamNetworkingIdentity; // hack around a weird bug of ToString crashin
            string buffer;
            localTemp.ToString(out buffer);
            return buffer;
        }        
    }
}
