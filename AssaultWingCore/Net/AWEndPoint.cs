using System.Net;
using AW2.Helpers;
using Steamworks;
using Microsoft.Xna.Framework;
using AW2.Core;

namespace AW2.Net
{
    public abstract class AWEndPoint {
        public static AWEndPoint Parse(GameServiceContainer Services, string text)
        {
            if (text.StartsWith("raw:")) {
                return AWEndPointRaw.ParseRaw(text.Substring(4)); // raw:host:udpport:tcpport
            } else {
                if (Services.GetService<SteamApiService>().Initialized) {
                    return new AWEndPointSteam(text); // ip:127.0.0.1:1234 steamid:12345671234512345
                } else {
                    throw new ArgumentException($"SteamAPI is not initialized. Can't attempt parsing endpoint {text} as a Steam Networking endpoint.");
                }
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
        public SteamNetworkingIdentity SteamNetworkingIdentity;
        public SteamNetworkingIPAddr DirectIp;
        public readonly bool UseDirectIp;

        public AWEndPointSteam(string parsed) {
            var directPrefix = "direct:";
            if (parsed.StartsWith(directPrefix)) {
                if (!DirectIp.ParseString(parsed.Substring(directPrefix.Length))) {
                    throw new ArgumentException($"Unknown direct IP address {parsed}. Example: direct:127.0.0.1:1234");
                }
                UseDirectIp = true;
            } else {
                if (!SteamNetworkingIdentity.ParseString(parsed)) {
                    throw new ArgumentException($"Unknown Steam Networking ID {parsed}. Example: ip:127.0.0.1:1234");
                }
            }
        }

        public override string ToString()
        {
            if (UseDirectIp) {
                SteamNetworkingIPAddr localTemp = DirectIp; // hack around a weird bug of ToString crashing
                string buffer;
                localTemp.ToString(out buffer, true);
                return buffer;
            }
            else {
                // Example: ip:10.10.10.10:123
                SteamNetworkingIdentity localTemp = SteamNetworkingIdentity; // hack around a weird bug of ToString crashing
                string buffer;
                localTemp.ToString(out buffer);
                return buffer;
            }
        }        
    }
}
