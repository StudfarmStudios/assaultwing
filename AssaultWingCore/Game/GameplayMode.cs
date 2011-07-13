using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Core;

namespace AW2.Game
{
    /// <summary>
    /// The entry of one player in a score table.
    /// </summary>
    public class Standing
    {
        public string Name { get; private set; }
        public Color PlayerColor { get; private set; }
        public bool IsRemote { get; private set; }
        public int Score { get; private set; }
        public int Kills { get; private set; }
        public int Deaths { get; private set; }
        public int SpectatorID { get; private set; }

        public Standing(string name, Color playerColor, bool isRemote, int score, int kills, int deaths, int spectatorID)
        {
            Name = name;
            PlayerColor = playerColor;
            IsRemote = isRemote;
            Score = score;
            Kills = kills;
            Deaths = deaths;
            SpectatorID = spectatorID;
        }
    }

    /// <summary>
    /// A bunch of parameters of the gameplay.
    /// </summary>
    public class GameplayMode
    {
        /// <summary>
        /// The types of ship available for selection in the gameplay mode.
        /// </summary>
        public string[] ShipTypes { get; set; }

        /// <summary>
        /// The types of extra devices available for selection in the gameplay mode.
        /// </summary>
        public string[] ExtraDeviceTypes { get; set; }

        /// <summary>
        /// The types of secondary weapon available for selection in the gameplay mode.
        /// </summary>
        public string[] Weapon2Types { get; set; }

        /// <summary>
        /// Number of lives of a player when starting a new arena.
        /// </summary>
        public int StartLives
        {
            get
            {
                return -1; // infinite lives
            }
        }

        public int CalculateScore(Player player)
        {
            return 2 * player.Kills - player.Deaths;
        }

        public IEnumerable<Standing> GetStandings(IEnumerable<Player> players)
        {
            return
                from p in players
                let score = CalculateScore(p)
                orderby score descending, p.Kills descending, p.Name
                select new Standing(p.Name, p.PlayerColor, p.IsRemote, score, p.Kills, p.Deaths, p.ID);
        }

        public bool ArenaFinished(Arena arena, IEnumerable<Player> players)
        {
            if (players.Count() < 2) return false;
            int playersAlive = players.Count(player => player.Lives != 0);
            return playersAlive < 2;
        }
    }
}
