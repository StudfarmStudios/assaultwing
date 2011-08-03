using System;

namespace AW2.Settings
{
    public class NetSettings
    {
        private string _managementServerAddress;
        private string _gameServerName;
        private int _gameServerMaxPlayers;
        private TimeSpan _dedicatedServerArenaTimeout;
        private TimeSpan _dedicatedServerArenaFinishCooldown;
        private string[] _dedicatedServerArenaNames;
        private TimeSpan[] _dedicatedServerArenaTimeoutMessages;

        public string ManagementServerAddress { get { return _managementServerAddress; } set { _managementServerAddress = value; } }
        public string GameServerName { get { return _gameServerName; } set { _gameServerName = value; } }
        public int GameServerMaxPlayers { get { return _gameServerMaxPlayers; } set { _gameServerMaxPlayers = value; } }
        public TimeSpan DedicatedServerArenaTimeout { get { return _dedicatedServerArenaTimeout; } set { _dedicatedServerArenaTimeout = value; } }
        public TimeSpan DedicatedServerArenaFinishCooldown { get { return _dedicatedServerArenaFinishCooldown; } set { _dedicatedServerArenaFinishCooldown = value; } }
        public string[] DedicatedServerArenaNames { get { return _dedicatedServerArenaNames; } set { _dedicatedServerArenaNames = value; } }
        public TimeSpan[] DedicatedServerArenaTimeoutMessages { get { return _dedicatedServerArenaTimeoutMessages; } set { _dedicatedServerArenaTimeoutMessages = value; } }

        public NetSettings()
        {
            Reset();
        }

        public void Reset()
        {
            _managementServerAddress = "assaultwing.com";
            _gameServerName = Environment.MachineName;
            _gameServerMaxPlayers = 16;
            _dedicatedServerArenaTimeout = TimeSpan.FromMinutes(15);
            _dedicatedServerArenaFinishCooldown = TimeSpan.FromSeconds(5);
            _dedicatedServerArenaNames = new string[0];
            _dedicatedServerArenaTimeoutMessages = new[]
            {
                TimeSpan.FromHours(1),
                TimeSpan.FromMinutes(30),
                TimeSpan.FromMinutes(20),
                TimeSpan.FromMinutes(10),
                TimeSpan.FromMinutes(5),
                TimeSpan.FromMinutes(1),
                TimeSpan.FromSeconds(10),
            };
        }
    }
}
