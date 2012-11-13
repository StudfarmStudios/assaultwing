using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
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

        /// <summary>
        /// For deserialization.
        /// </summary>
        public GameplayMode()
        {
        }

        public GameplayMode(float lifeScore, float killScore, float deathScore, float damageCombatPoints, float bonusesCombatPoints)
        {
            ScoreMultiplierLives = lifeScore;
            ScoreMultiplierKills = killScore;
            ScoreMultiplierDeaths = deathScore;
            CombatPointsMultiplierInflictedDamage = damageCombatPoints;
            CombatPointsMultiplierCollectedBonuses = bonusesCombatPoints;
        }

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
                where team.Members.Any()
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

        /// <summary>
        /// Returns a sequence of team member reassigning operations that will balance out the given teams.
        /// The returned tuples are (spectator ID, team ID) denoting which spectator should be assigned to which team.
        /// </summary>
        public IEnumerable<Tuple<int, int>> BalanceTeams(IEnumerable<Team> teams)
        {
            if (teams.Count() < 2) yield break;
            var ratingContext = new LocalRatingContext(CalculateScore, teams.SelectMany(team => team.Members));
            var localRatings = teams.SelectMany(team => team.Members).ToDictionary(spec => spec, ratingContext.Rate);
            var localTeamRatings = teams.ToDictionary(team => team, team => team.Members.Sum(spec => localRatings[spec]));
            var keepTeams = teams.OrderByDescending(team => localTeamRatings[team]).Take(2).ToArray();
            var keepSpecCount = keepTeams.Min(team => team.Members.Count() + 1);
            var freeSpecs = teams.SelectMany(team => team.Members).Except(keepTeams.SelectMany(team => team.Members.Take(keepSpecCount)));
            var teamRates = keepTeams.Select(team => team.Members.Take(keepSpecCount).Sum(spec => localRatings[spec])).ToArray();
            foreach (var spec in freeSpecs)
            {
                var index = Array.IndexOf(teamRates, teamRates.Min());
                yield return Tuple.Create(spec.ID, keepTeams[index].ID);
                teamRates[index] += localRatings[spec];
            }
        }

        public int RateLocally(Spectator spectator, IEnumerable<Spectator> allSpectators)
        {
            return new LocalRatingContext(CalculateScore, allSpectators).Rate(spectator);
        }

        private class LocalRatingContext
        {
            public const int LocalRatingMin = 1;
            public const int LocalRatingMax = 9;
            private const int ReliabilityTreshold = 7;

            public int MinScore { get; private set; }
            public int MaxScore { get; private set; }
            public float DampingFactor { get; private set; }
            public Func<ArenaStatistics, int> GetScore { get; private set; }

            public LocalRatingContext(Func<ArenaStatistics, int> getScore, IEnumerable<Spectator> allSpectators)
            {
                GetScore = getScore;
                var statistics = allSpectators.Select(spec => spec.LatestArenaStatistics);
                var scores = statistics.Select(getScore).OrderBy(score => score).ToArray();
                MinScore = scores[0];
                MaxScore = scores[scores.Length - 1];
                var reliability = statistics.Max(stats => Math.Max(stats.Kills, stats.Deaths));
                DampingFactor = MathHelper.Clamp(reliability / (float)ReliabilityTreshold, 0, 1);
            }

            /// <summary>
            /// Rates a spectator on a scale from <see cref="LocalRatingMin"/> to <see cref="LocalRatingMax"/>.
            /// The rating is based on the relative success of the spectator against others in the latest arena.
            /// </summary>
            public int Rate(Spectator spectator)
            {
                if (spectator.LatestArenaStatistics.IsEmpty) return (int)Math.Round((LocalRatingMin + LocalRatingMax) / 2f);
                var relativeScore = (GetScore(spectator.LatestArenaStatistics) - MinScore) / (float)(MaxScore - MinScore);
                var localRating = ((relativeScore - 0.5f) * DampingFactor + 0.5f) * (LocalRatingMax - LocalRatingMin) + LocalRatingMin;
                return (int)Math.Round(MathHelper.Clamp(localRating, LocalRatingMin, LocalRatingMax));
            }
        }
    }
}
