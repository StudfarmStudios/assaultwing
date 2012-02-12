using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
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

        public static string GetString(this JObject root, params string[] path)
        {
            if (path == null || path.Length == 0) throw new ArgumentException("Invalid JSON path");
            var element = root[path[0]];
            if (element == null) return "";
            foreach (var step in path.Skip(1))
            {
                if (element[step] == null) return "";
                element = element[step];
            }
            return element.ToString();
        }

        public static string GetString(this RegistryKey key, string name)
        {
            return (string)key.GetValue(name);
        }
    }
}
