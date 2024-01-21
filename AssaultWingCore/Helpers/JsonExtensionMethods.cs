using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Win32;
using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;
using AW2.Game;
using AW2.Game.Players;
using AW2.Net;

namespace AW2.Helpers
{
    public static class JsonExtensionMethods
    {

        /// <summary>
        /// Returns a string value from a JObject, or the empty string if some part of
        /// the path to the value doesn't exist.
        /// </summary>
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

    }
}
