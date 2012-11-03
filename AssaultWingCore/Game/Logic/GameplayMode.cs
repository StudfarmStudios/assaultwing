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

        public int CalculateScore(ArenaStatistics statistics)
        {
            return (CalculateCombatPoints(statistics) +
                ScoreMultiplierKills * statistics.Kills +
                ScoreMultiplierDeaths * statistics.Deaths +
                ScoreMultiplierLives * Math.Max(0, statistics.Lives)).Floor();
        }

        public float CalculateCombatPoints(ArenaStatistics statistics)
        {
            return CombatPointsMultiplierInflictedDamage * statistics.DamageInflictedToMinions +
                CombatPointsMultiplierCollectedBonuses * statistics.BonusesCollected;
        }

        public Standing[] GetStandings(IEnumerable<Spectator> spectators)
        {
            return (
                from spec in spectators
                let stats = spec.ArenaStatistics
                let score = CalculateScore(stats)
                orderby score descending, stats.Kills descending, spec.Name
                select new Standing(spec.ID, spec.Name, spec.Color, score, spec.ArenaStatistics, spec.StatsData,
                    isActive: spec.IsLocal || !spec.IsDisconnected)).ToArray();
        }

        /// <summary>
        /// Returns the standings. The standings may have two forms. The full form has all teams on the first level.
        /// The members of each team are listed in the substandings under the entry of the team.
        /// The condensed form is used when all teams have at most one member. The condensed form has all the
        /// players on the first level. Their substandings are empty.
        /// </summary>
        public Tuple<Standing, Standing[]>[] GetStandings(IEnumerable<Team> teams)
        {
            var standings = (
                from team in teams
                let stats = team.ArenaStatistics
                let score = CalculateScore(stats)
                orderby score descending, stats.Kills descending, team.Name
                let entry = new Standing(team.ID, team.Name, team.Color, score, team.ArenaStatistics, statsData: null, isActive: true)
                select Tuple.Create(entry, GetStandings(team.Members))).ToArray();
            if (teams.Any(team => team.Members.Count() > 1))
                return standings;
            else
                return (
                    from entry in standings
                    where entry.Item2.Length == 1
                    select Tuple.Create(entry.Item2[0], new Standing[0])).ToArray();
        }

        public bool ArenaFinished(Arena arena, IEnumerable<Spectator> spectators)
        {
            if (spectators.Count() < 2) return false;
            int spectatorsAlive = spectators.Count(player => player.ArenaStatistics.Lives != 0);
            return spectatorsAlive < 2;
        }
    }
}
