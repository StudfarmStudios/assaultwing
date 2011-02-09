using System;

namespace AW2.Settings
{
    public class NetSettings
    {
        private string _managementServerAddress;

        public string ManagementServerAddress { get { return _managementServerAddress; } set { _managementServerAddress = value; } }

        public NetSettings()
        {
            _managementServerAddress = "vs1164254.server4you.net";
        }
    }
}
