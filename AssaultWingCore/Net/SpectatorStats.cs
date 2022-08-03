using System;
using Newtonsoft.Json.Linq;
using AW2.Helpers.Serialization;
using AW2.Game.Players;

namespace AW2.Net
{
    /// <summary>
    /// Wrapper around JSON objects received from the stats server for a spectator.
    /// </summary>
    public class SpectatorStats
    {
        public string PilotId { get; set; }
        // TODO: Peter: SteamId based logged in mode with Steam, except in Raw mode
        public bool IsLoggedIn { get; set; }
        public float Rating { get; set; }
        public int RatingRank { get; set; }

        public SpectatorStats()
        {
        }
    }
}
