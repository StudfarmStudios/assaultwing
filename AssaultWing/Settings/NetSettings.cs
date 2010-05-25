using System;

namespace AW2.Settings
{
    public class NetSettings
    {
        private string _connectAddress;

        public string ConnectAddress { get { return _connectAddress; } set { _connectAddress = value; } }

        public NetSettings()
        {
#if DEBUG
            _connectAddress = "192.168.1.100";
#else
            _connectAddress = "82.181.78.56"; // to make life easier during closed beta testing
#endif
        }
    }
}
