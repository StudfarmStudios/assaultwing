using System;
using System.Collections.Generic;
using System.Linq;

namespace AW2.Game.Logic
{
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

        public int CalculateScore(SpectatorArenaStatistics statistics)
        {
            return 2 * statistics.Kills - statistics.Deaths;
        }

        public IEnumerable<Standing> GetStandings(IEnumerable<Spectator> spectators)
        {
            return
                from spec in spectators
                let stats = spec.ArenaStatistics
                let score = CalculateScore(stats)
                orderby score descending, stats.Kills descending, spec.Name
                select new Standing(spec, score);
        }

        public bool ArenaFinished(Arena arena, IEnumerable<Spectator> spectators)
        {
            if (spectators.Count() < 2) return false;
            int spectatorsAlive = spectators.Count(player => player.ArenaStatistics.Lives != 0);
            return spectatorsAlive < 2;
        }
    }
}
