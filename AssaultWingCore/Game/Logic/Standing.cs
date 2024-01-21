using System;
using AW2.Stats;
using Microsoft.Xna.Framework;

namespace AW2.Game.Logic
{
    /// <summary>
    /// An entry in a score table.
    /// </summary>
    public class Standing
    {
        public string Name { get; private set; }
        public Color Color { get; private set; }
        public bool IsActive { get; private set; }
        public int Score { get; private set; }
        public int Kills { get; private set; }
        public int Deaths { get; private set; }
        public PilotRanking Ranking { get; private set; }
        public int ID { get; private set; }

        public Standing(int id, string name, Color color, int score, ArenaStatistics arenaStatistics, PilotRanking ranking, bool isActive)
        {
            ID = id;
            Name = name;
            Color = color;
            IsActive = isActive;
            Score = score;
            Kills = arenaStatistics.Kills;
            Deaths = arenaStatistics.Deaths;
            Ranking = ranking;
        }
    }
}
