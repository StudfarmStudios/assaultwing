using System;
using AW2.Helpers.Serialization;

namespace AW2.Settings
{
    public class NetSettings
    {
        public string ManagementServerAddress { get; set; }
        public string DataServerAddress { get; set; }
        public string StatsServerAddress { get; set; }
        public int StatsHttpsPort { get; set; }
        public int StatsDataPort { get; set; }
        public bool StatsReportingEnabled { get; set; }
        public string GameServerName { get; set; }
        public int GameServerPort { get; set; }
        public int GameServerMaxPlayers { get; set; }
        public TimeSpan DedicatedServerArenaTimeout { get; set; }
        public TimeSpan DedicatedServerArenaFinishCooldown { get; set; }
        public string[] DedicatedServerArenaNames { get; set; }
        public TimeSpan[] DedicatedServerArenaTimeoutMessages { get; set; }
        public bool HeavyDebugLog { get; set; } // DEBUG: catch a rare crash that seems to happen only when serializing walls.
        public bool HeavyProfileLog { get; set; } // DEBUG
        public bool LagLog { get; set; } // DEBUG

        private bool IsDevPublish { get { return System.Reflection.Assembly.GetEntryAssembly().GetName().Name.EndsWith("Dev"); } }

        public NetSettings()
        {
            Reset();
        }

        public void Reset()
        {
            ManagementServerAddress = IsDevPublish ? "assaultwing.com:16728" : "assaultwing.com";
            DataServerAddress = "http://www.assaultwing.com";
            StatsServerAddress = "assaultwing.com";
            StatsHttpsPort =  IsDevPublish ? 4002 : 3002;
            StatsDataPort = IsDevPublish ? 4000 : 3000;
            StatsReportingEnabled = true;
            GameServerName = Environment.MachineName;
            GameServerPort = 'A' * 256 + 'W';
            GameServerMaxPlayers = 16;
            DedicatedServerArenaTimeout = TimeSpan.FromMinutes(15);
            DedicatedServerArenaFinishCooldown = TimeSpan.FromSeconds(5);
            DedicatedServerArenaNames = new string[0];
            DedicatedServerArenaTimeoutMessages = new[]
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
