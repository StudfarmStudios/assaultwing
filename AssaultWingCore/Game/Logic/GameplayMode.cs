// If ALL_VS_ALL is true then
// - Each player will be in their own team
// - If bots are included then there will be one BotPlayer in its own team
// If ALL_VS_ALL is false then
// - There will be two teams that are balanced based on human player rankings
// - If bots are included then there will be one BotPlayer in each team alongside human players
#define ALL_VS_ALL

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using AW2.Game.Players;
using AW2.Helpers;
using AW2.Settings;

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
        public Standings GetStandings(IEnumerable<Team> teams)
        {
            var standings = (
                from team in teams
                where team.Members.Any()
                let stats = team.ArenaStatistics
                let score = CalculateScore(stats)
                orderby score descending, stats.Kills descending, team.Name
                let entry = new Standing(team.ID, team.Name, team.Color, score, team.ArenaStatistics, statsData: null, isActive: true)
                select Tuple.Create(entry, GetStandings(team.Members))).ToArray();
            return new Standings(standings);
        }

        public bool ArenaFinished(Arena arena, IEnumerable<Spectator> spectators)
        {
            if (spectators.Count() < 2) return false;
            int spectatorsAlive = spectators.Count(player => player.ArenaStatistics.Lives != 0);
            return spectatorsAlive < 2;
        }

        /// <summary>
        /// Chooses the team for a spectator. The team may be one of the given ones or a new one.
        /// </summary>
        public TeamOperation ChooseTeam(Spectator spectator, IEnumerable<Team> allTeams)
        {
#if ALL_VS_ALL
            return TeamOperation.AssignToNewTeam(spectator.Name, spectator);
#else
            if (allTeams.Count() < 2) return TeamOperation.AssignToNewTeam(GetFreeTeamNames(allTeams).First(), spectator);
            var ratingContext = GetRatingContext(allTeams);
            return TeamOperation.AssignToExistingTeam(allTeams.OrderBy(ratingContext.Rate).First(), spectator);
#endif
        }

        /// <summary>
        /// Returns a sequence of operations that will balance out the given teams.
        /// </summary>
        public IEnumerable<TeamOperation> BalanceTeams(IEnumerable<Team> teams)
        {
#if ALL_VS_ALL
            yield break;
#else
            if (teams.Count() < 2) yield break;
            var ratingContext = GetRatingContext(teams);
            var spectators = teams
                .SelectMany(team => team.Members)
                .Where(spec => !(spec is BotPlayer))
                .Shuffle()
                .OrderByDescending(spec => ratingContext.Rate(spec))
                .ToArray();
            var resultTeams = teams.Take(2).ToDictionary(team => team, team => 0);
            foreach (var spec in spectators)
            {
                var weakestTeam = resultTeams.OrderBy(x => x.Value).First().Key;
                resultTeams[weakestTeam] += ratingContext.Rate(spec);
                yield return TeamOperation.AssignToExistingTeam(weakestTeam, spec);
            }
#endif
        }

        public int RateLocally(Spectator spectator, IEnumerable<Spectator> allSpectators)
        {
            return new LocalRatingContext(CalculateScore, allSpectators).Rate(spectator);
        }

        /// <summary>
        /// Returns operations that update the <see cref="BotPlayer"/> instances to conform to the settings.
        /// </summary>
        public IEnumerable<TeamOperation> UpdateBotPlayerConfiguration(IEnumerable<Team> teams, AWSettings settings)
        {
#if ALL_VS_ALL
            // Always remove bots from teams that have players.
            // If bots are excluded completely then also remove bots from their independent team.
            var removeExtraBots = teams
                .Where(team => !settings.Players.BotsEnabled || team.Members.OfType<Player>().Any())
                .SelectMany(team => team.Members.OfType<BotPlayer>())
                .Select(TeamOperation.Remove);
            bool hasIndependentBots = teams.Any(team => team.Members.All(spec => spec is BotPlayer));
            var addIndependentBots = !settings.Players.BotsEnabled || hasIndependentBots
                ? Array.Empty<TeamOperation>()
                : new[] { TeamOperation.CreateToNewTeam("Bots", "Bots") };
            return removeExtraBots.Union(addIndependentBots);
#else
            var removeExtraBots = teams
                .SelectMany(team => team.Members.OfType<BotPlayer>().Skip(1))
                .Select(botPlayer => TeamOperation.Remove(botPlayer));
            var switchOnOrOff = settings.Players.BotsEnabled
                ? teams
                    .Where(team => !team.Members.OfType<BotPlayer>().Any())
                    .Select(team => TeamOperation.CreateToExistingTeam(team, GetBotPlayerName(team.Name)))
                : teams
                    .SelectMany(team => team.Members.OfType<BotPlayer>())
                    .Select(botPlayer => TeamOperation.Remove(botPlayer));
            var addMissingTeams = settings.Players.BotsEnabled
                ? GetFreeTeamNames(teams).Take(2 - teams.Count()).Select(name => TeamOperation.CreateToNewTeam(name, GetBotPlayerName(name)))
                : new TeamOperation[0];
            return removeExtraBots.Union(switchOnOrOff).Union(addMissingTeams);
#endif
        }

        private string GetBotPlayerName(string teamName)
        {
            return teamName.TrimEnd('s') + " Bots";
        }

        private LocalRatingContext GetRatingContext(IEnumerable<Team> teams)
        {
            return new LocalRatingContext(CalculateScore, teams.SelectMany(team => team.Members));
        }

        private IEnumerable<string> GetFreeTeamNames(IEnumerable<Team> teams)
        {
            return GetTeamNames().Except(teams.Select(p => p.Name));
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
