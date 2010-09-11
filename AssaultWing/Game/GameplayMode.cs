using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using AW2.Core;

namespace AW2.Game
{
    /// <summary>
    /// The entry of one player in a score table.
    /// </summary>
    public struct Standing
    {
        public string Name { get; set; }
        public Color PlayerColor { get; set; }
        public int Score { get; set; }
        public int Kills { get; set; }
        public int Suicides { get; set; }
        public int SpectatorId { get; set; }
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
                return AssaultWingCore.Instance.NetworkMode == NetworkMode.Standalone
                    ? 3 // standalone games have three lives
                    : -1; // network games have infinite lives
            }
        }

        /// <summary>
        /// Calculates the score of a player
        /// </summary>
        public int CalculateScore(Player player)
        {
            return player.Kills - player.Suicides;
        }

        /// <summary>
        /// Returns the standings of players.
        /// </summary>
        public IEnumerable<Standing> GetStandings(IEnumerable<Player> players)
        {
            return
                from p in players
                let score = CalculateScore(p)
                orderby score descending
                select new Standing
                {
                    Name = p.Name,
                    PlayerColor = p.PlayerColor,
                    Score = score,
                    Kills = p.Kills,
                    Suicides = p.Suicides,
                    SpectatorId = p.ID
                };
        }

        /// <summary>
        /// Have players finished playing an arena.
        /// </summary>
        public bool ArenaFinished(Arena arena, IEnumerable<Player> players)
        {
            if (players.Count() < 2) return false;
            int playersAlive = players.Count(player => player.Lives != 0);
            return playersAlive < 2;
        }
    }
}
