using System.Globalization;
using AW2.Core;
using AW2.Helpers;

namespace AW2.Stats
{

    /// <summary>
    /// Runs on client and check to see if local player's PilotRanking needs to be uploaded to steam leaderboard
    /// </summary>
    public class LocalPilotRankingHandler : AWGameComponent
    {

        private SteamLeaderboardService SteamLeaderboardService => Game.Services.GetService<SteamLeaderboardService>();

        /// <summary>
        /// Current best information of local player's ranking.
        /// </summary>
        public PilotRanking LocalPilotRanking { get; private set; }

        /// <summary>
        /// Used to throttle the fetching of fresh ranking from Steam.
        /// </summary>
        private DateTime LastFetchAttempted = DateTime.MinValue;

        /// <summary>
        /// After upload, other clients may be uploading so it takes a while for the ranking of all players to settle.
        /// For this reason we keep track of the last uploaded ranking and use it as a base for timing a fetching of fresh
        /// ranking a bit later.
        /// </summary>
        private DateTime ScheduledRankingFetch = DateTime.MaxValue;

        public LocalPilotRankingHandler(AssaultWingCore game) : base(game, 100)
        {
        }

        /// <summary>
        /// Checks if local player has been awarded a new rating and if so, uploads it to the Steam leaderboard. Try to
        /// fetch fresh ranking if it makes sense.
        /// </summary>
        override public void Update()
        {
            var localPlayer = Game.DataEngine.LocalPlayer;

            if (localPlayer is null || !Game.IsSteam)
            {
                return; // A stand in local player is attempted to be created in UserControlledLogic, but safer to check anyway.
            }

            var localPlayerRanking = localPlayer.Ranking;

            if (!SteamLeaderboardService.IsUploadingRanking && localPlayerRanking.IsValid &&
               localPlayerRanking.RatingAwardedTime > LocalPilotRanking.RatingAwardedTime)
            {
                Log.Write($"LocalPilotRankingHandler: Ranking update from Server {LocalPilotRanking} -> {localPlayerRanking}. Uploading to steam.");
                LocalPilotRanking = localPlayerRanking;
                var uploadResult = SteamLeaderboardService.UploadCurrentPilotRanking(localPlayerRanking, (updatedRanking, errors) =>
                {
                    if (errors)
                    {
                        Log.Write($"LocalPilotRankingHandler: Tried to upload rating, but error occured.");
                    }
                    else
                    {
                        // Schedule a "fetch" around 3 seconds after rating was awarded to have a good chance of getting fresh ranking from Steam
                        // that includes the updates from other clients also.
                        ScheduledRankingFetch = DateTime.UtcNow.AddSeconds(3);

                        if (LocalPilotRanking == localPlayerRanking)
                        {
                            Log.Write($"LocalPilotRankingHandler: Rating uploaded and rank updated: {LocalPilotRanking.RankString} -> {updatedRanking.RankString}");
                            LocalPilotRanking = updatedRanking; // We may have gotten a new rank from Steam.
                        }
                        else
                        {
                            Log.Write($"LocalPilotRankingHandler: Rating uploaded but ranking information changed simultaneously.");
                        }
                    }
                });
                if (!uploadResult)
                {
                    Log.Write("LocalPilotRankingHandler: Rating upload not possible.");
                }
            }
            else if (!LocalPilotRanking.IsValid && LastFetchAttempted.AddSeconds(360) < DateTime.UtcNow)
            {
                Log.Write($"LocalPilotRankingHandler: No valid local pilot ranking. Attempting to fetch.");
                FetchLocalPlayerRanking();
            }
            else if (!SteamLeaderboardService.IsUploadingRanking &&
                !SteamLeaderboardService.IsGettingRankings &&
                ScheduledRankingFetch < DateTime.UtcNow)
            {
                ScheduledRankingFetch = DateTime.MaxValue;
                Log.Write($"LocalPilotRankingHandler: Scheduled ranking fetch.");
                FetchLocalPlayerRanking();
            }
        }

        private void FetchLocalPlayerRanking()
        {
            if (!Game.IsSteam)
            {
                return;
            }

            if (SteamLeaderboardService.IsGettingRankings)
            {
                Log.Write($"LocalPilotRankingHandler: Skip fetch. Already getting rankings from Steam. Previous attempt {LastFetchAttempted.ToString("s", DateTimeFormatInfo.InvariantInfo)}");
                return;
            }
            else
            {
                Log.Write($"LocalPilotRankingHandler: Attempting to fetch local player ranking from Steam. Previous attempt {LastFetchAttempted.ToString("s", DateTimeFormatInfo.InvariantInfo)}");
                LastFetchAttempted = DateTime.UtcNow;
            }

            var localRankingOriginal = LocalPilotRanking;

            SteamLeaderboardService.GetCurrentPlayerRanking((SteamLeaderboardService.PlayerRankingResultDelegate)((ranking, errors) =>
            {
                if (errors)
                {
                    Log.Write("LocalPilotRankingHandler: Error getting Ranking from Steam");
                }
                else
                {
                    if (this.LocalPilotRanking == localRankingOriginal)
                    {
                        if (localRankingOriginal == ranking)
                        {
                            Log.Write($"LocalPilotRankingHandler: Fresh ranking from Steam is same: {ranking}");
                        }
                        else
                        {
                            Log.Write($"LocalPilotRankingHandler: Fresh ranking from Steam: {ranking}");
                            this.LocalPilotRanking = ranking; // Don't upload the ranking if we already got it from Steam.
                        }
                    }
                    else
                    {
                        Log.Write("LocalPilotRankingHandler: Local ranking updated otherwise while fetching from Steam");
                    }
                }
            }));
        }
    }
}
