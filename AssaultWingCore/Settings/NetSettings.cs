using System;

namespace AW2.Settings
{
    public class NetSettings
    {
        private string _managementServerAddress;
        private string _dataServerAddress;
        private string _statsServerAddress;
        private int _statsHttpsPort;
        private int _statsDataPort;
        private bool _statsReportingEnabled;
        private string _gameServerName;
        private int _gameServerPort;
        private int _gameServerMaxPlayers;
        private TimeSpan _dedicatedServerArenaTimeout;
        private TimeSpan _dedicatedServerArenaFinishCooldown;
        private string[] _dedicatedServerArenaNames;
        private TimeSpan[] _dedicatedServerArenaTimeoutMessages;
        private bool _heavyDebugLog; // DEBUG: catch a rare crash that seems to happen only when serializing walls.
        private bool _heavyProfileLog; // DEBUG
        private bool _lagLog; // DEBUG

        public string ManagementServerAddress { get { return _managementServerAddress; } set { _managementServerAddress = value; } }
        public string DataServerAddress { get { return _dataServerAddress; } set { _dataServerAddress = value; } }
        public string StatsServerAddress { get { return _statsServerAddress; } set { _statsServerAddress = value; } }
        public int StatsHttpsPort { get { return _statsHttpsPort; } set { _statsHttpsPort = value; } }
        public int StatsDataPort { get { return _statsDataPort; } set { _statsDataPort = value; } }
        public bool StatsReportingEnabled { get { return _statsReportingEnabled; } set { _statsReportingEnabled = value; } }
        public string GameServerName { get { return _gameServerName; } set { _gameServerName = value; } }
        public int GameServerPort { get { return _gameServerPort; } set { _gameServerPort = value; } }
        public int GameServerMaxPlayers { get { return _gameServerMaxPlayers; } set { _gameServerMaxPlayers = value; } }
        public TimeSpan DedicatedServerArenaTimeout { get { return _dedicatedServerArenaTimeout; } set { _dedicatedServerArenaTimeout = value; } }
        public TimeSpan DedicatedServerArenaFinishCooldown { get { return _dedicatedServerArenaFinishCooldown; } set { _dedicatedServerArenaFinishCooldown = value; } }
        public string[] DedicatedServerArenaNames { get { return _dedicatedServerArenaNames; } set { _dedicatedServerArenaNames = value; } }
        public TimeSpan[] DedicatedServerArenaTimeoutMessages { get { return _dedicatedServerArenaTimeoutMessages; } set { _dedicatedServerArenaTimeoutMessages = value; } }
        public bool HeavyDebugLog { get { return _heavyDebugLog; } set { _heavyDebugLog = value; } } // DEBUG: catch a rare crash that seems to happen only when serializing walls.
        public bool HeavyProfileLog { get { return _heavyProfileLog; } set { _heavyProfileLog = value; } } // DEBUG
        public bool LagLog { get { return _lagLog; } set { _lagLog = value; } } // DEBUG

        private bool IsDevPublish { get { return System.Reflection.Assembly.GetEntryAssembly().GetName().Name.EndsWith("Dev"); } }

        public NetSettings()
        {
            Reset();
        }

        public void Reset()
        {
            _managementServerAddress = IsDevPublish ? "assaultwing.com:16728" : "assaultwing.com";
            _dataServerAddress = "http://www.assaultwing.com";
            _statsServerAddress = "assaultwing.com";
            _statsHttpsPort =  IsDevPublish ? 4002 : 3002;
            _statsDataPort = IsDevPublish ? 4000 : 3000;
            _statsReportingEnabled = true;
            _gameServerName = Environment.MachineName;
            _gameServerPort = 'A' * 256 + 'W';
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
