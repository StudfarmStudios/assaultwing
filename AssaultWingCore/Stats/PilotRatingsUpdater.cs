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

        public PilotRatingsUpdater(AssaultWingCore game) : base(game, 100)
        {
        }

        private CSteamID? GetSteamId(Player player) => Game.NetworkEngine.GetSteamId(player.ConnectionID);


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
                .OfType<(EloRating<CSteamID> Input, bool Update)>().ToArray();

            HashSet<CSteamID> updateRating =
                ratingInputs.Where(p => p.Update).Select(p => p.Input.PlayerId).ToHashSet();

            if (ratingInputs.Length < 2)
            {
                Log.Write($"Not updating pilot ratings due to not enough players with valid state.");
                return;
            }

            Dictionary<CSteamID, EloRating<CSteamID>> updatedRatings =
                multiElo
                .Update<CSteamID>(ratingInputs.Select(p => p.Input).ToArray())
                .ToDictionary(r => r.PlayerId);

            List<String> updateLogMessages = new List<String>();

            foreach (var player in players)
            {
                var steamId = GetSteamId(player);
                if (steamId is not null)
                {
                    if (updatedRatings.ContainsKey(steamId.Value) && updateRating.Contains(steamId.Value))
                    {
                        var updatedRating = updatedRatings[steamId.Value];
                        var prevRanking = player.Ranking;
                        // note that the rank is not accurate at this point, but it is still better than nothing.
                        var newRating = new PilotRanking
                        {
                            State = PilotRanking.StateType.RatingCalculated,
                            Rating = updatedRating.Rating,
                            RatingAwardedTime = now,
                        };
                        player.Ranking = player.Ranking.Merge(newRating);
                        updateLogMessages.Add($"[{player.Name}: {prevRanking} -> {player.Ranking}");
                    }
                }
            }

            Log.Write($"Updated {updateLogMessages.Count} pilot ratings: {String.Join(", ", updateLogMessages)}");
        }

        private (EloRating<CSteamID> Input, bool Update)? GetEloRatingInput(Player player, Standings standings)
        {
            var standing = standings.FindForSpectator(player);

            CSteamID? steamId = GetSteamId(player);
            if (steamId is null)
            {
                Log.Write($"Player {player.Name} has no Steam ID. Not updating ratings.");
                // Returning null from this function causes the rating work as if this player didn't exist.
                // Also the rating of this player will not be updated.
                return null;
            }
            if (standing is null)
            {
                Log.Write($"Player {player.Name} has no standing information. Not updating ratings.");
                return null;
            }

            var currentRanking = player.Ranking;

            (int current, bool update, string? message) = currentRanking.State switch
            {
                PilotRanking.StateType.RatingCalculated =>
                    (currentRanking.Rating, true, null),
                PilotRanking.StateType.RankingDownloaded =>
                    (currentRanking.Rating, true, null),
                PilotRanking.StateType.NotRatedYet =>
                    (1000, true, $"Player {player.Name} has no rating/ranking information yet. Starting with rating of 1000."),
                _ =>
                    // In case of failure to download rating or similar we can update others rating assuming this 
                    // player has 0 rating, but we shouldn't update this players rating.
                    (0, false, $"Player {player.Name} has invalid rating/ranking information. Assuming rating 0. Not updating rating for this player. {currentRanking}")
            };

            if (message is not null)
            {
                Log.Write(message);
            }

            return (new EloRating<CSteamID>() { PlayerId = steamId.Value, Rating = current, Score = standing.Score }, update);
        }
    }
}
