using Steamworks;

namespace AW2.Helpers {
    public class Steam {
        public static string IpAddrToString(SteamNetworkingIPAddr addr) {
            string buffer;
            addr.ToString(out buffer, true);
            return buffer;
        }

        // Example: ip:10.10.10.10:123
        public static string IdentityToString(SteamNetworkingIdentity identity) {
            string buffer;
            identity.ToString(out buffer);
            return buffer;
        }

    }
}