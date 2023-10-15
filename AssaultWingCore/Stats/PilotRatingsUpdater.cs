using System.Globalization;
using AW2.Core;
using AW2.Game.Logic;
using AW2.Game.Players;
using Steamworks;
using AW2.Helpers;

namespace AW2.Stats
{
    /// <summary>
    /// Runs on the server and updates the PilotRanking of each player in the arena.
    /// </summary>
    public class PilotRatingsUpdater : AWGameComponent
    {

        /// <summary>
        /// Initialize a multi elo rating system with rather standard parameters.
        /// </summary>
        private static readonly MultiElo multiElo = new MultiElo(k: 32, d: 400, logBase: 10);

        private SteamLeaderboardService SteamLeaderboardService => Game.Services.GetService<SteamLeaderboardService>();

        /// <summary>
        /// Used on server to keep track of which players have had their ranking downloaded.
        /// </summary>
        private HashSet<CSteamID> RatingFetchStarted = new HashSet<CSteamID>();
        private List<CSteamID> RatingFetchOngoing = new List<CSteamID>();
        private HashSet<CSteamID> RatingFetchFinished = new HashSet<CSteamID>();
        private HashSet<CSteamID> BlockRatingUpdateForPlayers = new HashSet<CSteamID>();

        public PilotRatingsUpdater(AssaultWingCore game) : base(game, 100)
        {
        }


        private CSteamID? GetSteamId(Player player) => Game.NetworkEngine.GetSteamId(player.ConnectionID);

        /// <summary>
        /// At the start of each round server initiates the fetching of fresh ranking information from the Steam for
        /// the players connected at that time.
        /// </summary>
        public void StartArena()
        {
            List<CSteamID> steamIds = Game.DataEngine.Players.Select(GetSteamId).OfType<CSteamID>().ToList();

            // Clear state about which players have had their ranking downloaded. A rankings query could be
            // ongoing if the round ended while previous download was ongoing, but that does not cause any
            // confusion, because RatingFetchOngoing is cleared here and the results will be ignored.
            RatingFetchStarted.Clear();
            RatingFetchOngoing.Clear();
            RatingFetchFinished.Clear();
            BlockRatingUpdateForPlayers.Clear();

            Update();
        }

        /// <summary>
        /// Handle new players joining the game during the arena.
        /// </summary>
        override public void Update()
        {
            if (!SteamLeaderboardService.IsGettingRankings)
            {
                // Only 1 call allowed in progress at a time.

                // Players who have joined since the start of the round.
                List<CSteamID> steamIds =
                    Game.DataEngine.Players
                    .Select(GetSteamId)
                    .OfType<CSteamID>()
                    .Where(id => !RatingFetchStarted.Contains(id)).ToList();

                if (steamIds.Count == 0) return;

                RatingFetchOngoing = steamIds;

                foreach (var steamId in steamIds)
                {
                    RatingFetchStarted.Add(steamId); // Only try to fetch once per arena per player.
                }

                Log.Write($"Downloading ranking information for {steamIds.Count} new players.");
                SteamLeaderboardService.GetRankings(steamIds, HandlePilotRankings);
            }
        }

        /// <summary>
        /// At end of round on the server, compute new rating scores. These will
        /// then be sent to clients which will upload the new rating to Steam
        /// leaderboard and thus possibly get an updated rank from the leaderboard.
        /// </summary>
        public void EndArena(Standings standings)
        {
            if (!Game.DataEngine.GameplayMode.AllVsAll)
            {
                Log.Write("Not updating pilot ratings because game mode is not all-vs-all.");
                // TODO: Support team aware rating algorithm? This algorithm was once considered https://www.microsoft.com/en-us/research/project/trueskill-ranking-system/
                return;
            }

            if (Game.Settings.Players.BotsEnabled)
            {
                Log.Write("Not updating pilot ratings because bots are enabled.");
                return;
            }

            List<Player> players = Game.DataEngine.Players.ToList();

            if (players.Count < 2)
            {
                Log.Write($"Not updating pilot ratings due to player count being {players.Count}.");
                return;
            }

            var now = DateTime.UtcNow;

            var ratingInputs = players
                .Select(p => GetEloRatingInput(p, standings))
                .OfType<EloRating<CSteamID>>().ToArray();

            if (ratingInputs.Length < 2)
            {
                Log.Write($"Not updating pilot ratings due to not enough players with valid state.");
                return;
            }

            Dictionary<CSteamID, EloRating<CSteamID>> updatedRatings =
                multiElo
                .Update<CSteamID>(ratingInputs)
                .ToDictionary(r => r.PlayerId);

            List<String> updateLogMessages = new List<String>();

            foreach (var player in players)
            {
                var steamId = GetSteamId(player);
                if (steamId is not null)
                {
                    if (updatedRatings.ContainsKey(steamId.Value) && !BlockRatingUpdateForPlayers.Contains(steamId.Value))
                    {
                        var updatedRating = updatedRatings[steamId.Value];
                        var prevRanking = player.Ranking;
                        player.Ranking = player.Ranking.UpdateRating(updatedRating.Rating, now);
                        updateLogMessages.Add($"[{player.Name}: {prevRanking.Rating} -> {player.Ranking.Rating}, score: {updatedRating.Score}, old awarded: {prevRanking.RatingAwardedTime.ToString("s", DateTimeFormatInfo.InvariantInfo)}]");
                    }
                }
            }

            Log.Write($"Updated {updateLogMessages.Count} pilot ratings: {String.Join(", ", updateLogMessages)}");
        }

        private EloRating<CSteamID>? GetEloRatingInput(Player player, Standings standings)
        {
            var standing = standings.FindForSpectator(player);

            CSteamID? steamId = GetSteamId(player);
            if (steamId is null)
            {
                Log.Write("Player {player.Name} has no Steam ID. Not updating ratings.");
                // Returning null from this function causes the rating work as if this player didn't exist.
                // Also the rating of this player will not be updated.
                return null;
            }
            if (standing is null)
            {
                Log.Write("Player {player.Name} has no standing information. Not updating ratings.");
                return null;
            }

            var currentRanking = player.Ranking;

            int currentRating = 0;

            if (currentRanking.IsValid)
            {
                currentRating = currentRanking.Rating;
            }
            else
            {
                if (RatingFetchFinished.Contains(steamId.Value))
                {
                    // Fresh players start with 1000 rating. 
                    currentRating = 1000;
                    Log.Write($"Player {player.Name} has no valid rating/ranking information. Starting with rating of 1000.");
                }
                else
                {
                    // In case of failure to download rating we can update others rating assuming this 
                    // player has 0 rating, but we shouldn't update this players rating.
                    Log.Write($"Player {player.Name} has no valid previously known rating and no rating information downloaded. Assuming rating 0 and not updating rating for this player.");
                    currentRating = 0;
                    BlockRatingUpdateForPlayers.Add(steamId.Value);
                }
            }

            return new EloRating<CSteamID>() { PlayerId = steamId.Value, Rating = currentRating, Score = standing.Score };
        }

        private void HandlePilotRankings(Dictionary<CSteamID, PilotRanking> rankings, bool errors)
        {
            // Fresh players won't have a rating.
            Log.Write($"Received {rankings.Count} pilot rankings from Steam leaderboard. Requested {RatingFetchOngoing.Count}.");

            foreach (var ranking in rankings)
            {
                var steamId = ranking.Key;
                var pilotRanking = ranking.Value;
                var player = Game.DataEngine.Players.FirstOrDefault(p => GetSteamId(p) == steamId);
                if (player is not null && pilotRanking.IsValid)
                {
                    // If the update of new rankings to steam is lagging, the server may have newer data than
                    // what can be downloaded from Steam. In that case, don't overwrite the server data.
                    // However note, that the AwardedTime can be the same, but the rank may be updated.
                    if (player.Ranking.RatingAwardedTime < pilotRanking.RatingAwardedTime)
                    {
                        player.Ranking = pilotRanking;
                        Log.Write($"Pilot {player.Name} received updated rating and ranking information {player.Ranking}");
                    }
                    else if (player.Ranking.RatingAwardedTime == pilotRanking.RatingAwardedTime && pilotRanking.Rank != player.Ranking.Rank)
                    {
                        Log.Write($"Pilot {player.Name} has the latest rating, but rank is updated {player.Ranking.Rank} -> {pilotRanking.Rank}");
                        player.Ranking = pilotRanking;
                    }
                    else
                    {
                        Log.Write($"Pilot {player.Name} ranking information is already up to date");
                    }
                }
                else
                {
                    Log.Write($"Pilot {player?.Name}, failed to apply received ranking information {pilotRanking}");
                }
            }

            foreach (var steamId in RatingFetchOngoing)
            {
                RatingFetchFinished.Add(steamId);
            }

            if (errors)
            {
                Log.Write($"Errors occured while fetching pilot rankings for {RatingFetchOngoing.Count} players. As a safety precaution, their ratings won't be updated.");
                BlockRatingUpdateForPlayers.UnionWith(RatingFetchOngoing);
            }

            RatingFetchOngoing.Clear();
        }
    }
}
