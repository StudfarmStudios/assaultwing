using System;
using Microsoft.Xna.Framework;

namespace AW2.Game.Logic
{
    /// <summary>
    /// The entry of one player in a score table.
    /// </summary>
    public class Standing
    {
        public string Name { get; private set; }
        public Color Color { get; private set; }
        public bool IsLocal { get; private set; }
        public bool IsDisconnected { get; private set; }
        public int Score { get; private set; }
        public int Kills { get; private set; }
        public int Deaths { get; private set; }
        public float Rating { get; private set; }
        public int SpectatorID { get; private set; }
        public object StatsData { get; private set; }

        public Standing(Spectator spec, int score)
        {
            Name = spec.Name;
            Color = spec.Color;
            IsLocal = spec.IsLocal;
            IsDisconnected = spec.IsDisconnected;
            Score = score;
            Kills = spec.ArenaStatistics.Kills;
            Deaths = spec.ArenaStatistics.Deaths;
            Rating = spec.ArenaStatistics.Rating();
            SpectatorID = spec.ID;
            StatsData = spec.StatsData;
        }
    }
}
