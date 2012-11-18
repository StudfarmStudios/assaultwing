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
        /// Chooses the team a spectator. The team may be one of the given ones or a new one.
        /// </summary>
        public TeamChoice ChooseTeam(Spectator spectator, IEnumerable<Team> allTeams)
        {
            if (allTeams.Count() < 2) return new TeamChoice(GetFreeTeamName(allTeams));
            var ratingContext = GetRatingContext(allTeams);
            return new TeamChoice(allTeams.OrderBy(ratingContext.Rate).First());
        }

        /// <summary>
        /// Returns a sequence of team member reassigning operations that will balance out the given teams.
        /// The returned tuples are (spectator ID, team ID) denoting which spectator should be assigned to which team.
        /// </summary>
        public IEnumerable<Tuple<int, int>> BalanceTeams(IEnumerable<Team> teams)
        {
            if (teams.Count() < 2) yield break;
            var ratingContext = GetRatingContext(teams);
            var localRatings = teams.SelectMany(team => team.Members).ToDictionary(spec => spec, ratingContext.Rate);
            var localTeamRatings = teams.ToDictionary(team => team, ratingContext.Rate);
            var relevantTeamCount = Math.Max(2, teams.Count(team => team.Members.Any()));
            var teamsOrdered = teams.OrderByDescending(team => localTeamRatings[team]).Take(relevantTeamCount).ToArray();
            if (teamsOrdered.Length > 2)
            {
                // Merge small teams into the two biggest ones.
                var team1 = teamsOrdered[0];
                var team2 = teamsOrdered[1];
                foreach (var team in teamsOrdered.Skip(2))
                    foreach (var spec in team.Members)
                    {
                        var weakestTeam = localTeamRatings[team1] < localTeamRatings[team2] ? team1 : team2;
                        localTeamRatings[weakestTeam] += localRatings[spec];
                        yield return Tuple.Create(spec.ID, weakestTeam.ID);
                    }
            }
            else
            {
                // Balance teams by reassigning spectators.
                var weakTeam = localTeamRatings[teamsOrdered[0]] < localTeamRatings[teamsOrdered[1]] ? teamsOrdered[0] : teamsOrdered[1];
                var strongTeam = localTeamRatings[teamsOrdered[0]] < localTeamRatings[teamsOrdered[1]] ? teamsOrdered[1] : teamsOrdered[0];
                if (strongTeam.Members.Count() > 1)
                {
                    var ratingDifference = localTeamRatings[strongTeam] - localTeamRatings[weakTeam];
                    var reassignee = strongTeam.Members.OrderBy(spec => Math.Abs(ratingDifference - 2 * localRatings[spec])).First();
                    if (Math.Abs(ratingDifference - 2 * localRatings[reassignee]) < ratingDifference - 1) // Avoid near trivial reassignments.
                        yield return Tuple.Create(reassignee.ID, weakTeam.ID);
                }
            }
        }

        public int RateLocally(Spectator spectator, IEnumerable<Spectator> allSpectators)
        {
            return new LocalRatingContext(CalculateScore, allSpectators).Rate(spectator);
        }

        private LocalRatingContext GetRatingContext(IEnumerable<Team> teams)
        {
            return new LocalRatingContext(CalculateScore, teams.SelectMany(team => team.Members));
        }

        private string GetFreeTeamName(IEnumerable<Team> teams)
        {
            return GetTeamNames().Except(teams.Select(p => p.Name)).First();
        }

        private IEnumerable<string> GetTeamNames()
        {
            yield return "Avengers";
            yield return "Vindicators";
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
                if (spectator.LatestArenaStatistics.IsEmpty || MinScore == MaxScore) return (int)Math.Round((LocalRatingMin + LocalRatingMax) / 2f);
                var relativeScore = (GetScore(spectator.LatestArenaStatistics) - MinScore) / (float)(MaxScore - MinScore);
                var localRating = ((relativeScore - 0.5f) * DampingFactor + 0.5f) * (LocalRatingMax - LocalRatingMin) + LocalRatingMin;
                return (int)Math.Round(MathHelper.Clamp(localRating, LocalRatingMin, LocalRatingMax));
            }

            public int Rate(Team team)
            {
                return team.Members.Sum(spec => Rate(spec));
            }
        }
    }
}
