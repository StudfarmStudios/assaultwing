using System;
using System.Collections.Generic;
using System.Linq;
using AW2.Game.Players;
using AW2.Helpers;

namespace AW2.Game.Logic
{
    /// <summary>
    /// A bunch of parameters of the gameplay.
    /// </summary>
    public class GameplayMode
    {
        public CanonicalString Name { get; private set; }

        /// <summary>
        /// The arenas available for play in the gameplay mode.
        /// </summary>
        public string[] Arenas { get; private set; }

        /// <summary>
        /// The types of ship available for selection in the gameplay mode.
        /// </summary>
        public string[] ShipTypes { get; private set; }

        /// <summary>
        /// The types of extra devices available for selection in the gameplay mode.
        /// </summary>
        public string[] ExtraDeviceTypes { get; private set; }

        /// <summary>
        /// The types of secondary weapon available for selection in the gameplay mode.
        /// </summary>
        public string[] Weapon2Types { get; private set; }

        /// <summary>
        /// Number of lives of a player when starting a new arena, or -1 for infinite lives.
        /// </summary>
        public int StartLives { get; private set; }

        public float ScoreMultiplierLives { get; private set; }
        public float ScoreMultiplierKills { get; private set; }
        public float ScoreMultiplierDeaths { get; private set; }
        public float CombatPointsMultiplierInflictedDamage { get; private set; }
        public float CombatPointsMultiplierCollectedBonuses { get; private set; }

        public int CalculateScore(SpectatorArenaStatistics statistics)
        {
            return (int)(0.001f + // Truncate to integer but allow for slight floating-point rounding error.
                CalculateCombatPoints(statistics) +
                ScoreMultiplierKills * statistics.Kills +
                ScoreMultiplierDeaths * statistics.Deaths +
                ScoreMultiplierLives * Math.Max(0, statistics.Lives));
        }

        public float CalculateCombatPoints(SpectatorArenaStatistics statistics)
        {
            return CombatPointsMultiplierInflictedDamage * statistics.DamageInflictedToMinions +
                CombatPointsMultiplierCollectedBonuses * statistics.BonusesCollected;
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
