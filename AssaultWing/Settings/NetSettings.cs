using System;

namespace AW2.Settings
{
    public class NetSettings
    {
        private string _connectAddress;

        public string ConnectAddress { get { return _connectAddress; } set { _connectAddress = value; } }

        public NetSettings()
        {
            _connectAddress = "192.168.1.100";
        }
    }
}
