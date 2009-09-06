using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AW2.Game
{
    /// <summary>
    /// The entry of one player in a score table.
    /// </summary>
    public struct Standing
    {
        public string Name { get; set; }
        public int Score { get; set; }
        public int Kills { get; set; }
        public int Suicides { get; set; }
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
        /// The types of primary weapon available for selection in the gameplay mode.
        /// </summary>
        public string[] Weapon1Types { get; set; }

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
                return AssaultWing.Instance.NetworkMode == NetworkMode.Standalone
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
                select new Standing { Name = p.Name, Score = score, Kills = p.Kills, Suicides = p.Suicides };
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
