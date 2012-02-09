using System;
using System.Collections.Generic;
using System.Linq;
using AW2.Game;
using AW2.Net;

namespace AW2.Helpers
{
    public static class ExtensionMethods
    {
        public static SpectatorStats GetStats(this Spectator spectator)
        {
            if (spectator.StatsData == null) spectator.StatsData = new SpectatorStats();
            return (SpectatorStats)spectator.StatsData;
        }
    }
}
