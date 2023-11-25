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

        /// <summary>
        /// Track whether the latest rating is uploaded to steam. This is used to avoid uploading the same rating
        /// multiple times.
        /// </summary>
        private PilotRanking LastKnownSteamRanking;

        private int fetchErrorCount = 0;

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

            // Sync both ways. In some corner case we may have downloade up to
            // date ranking, but the server may also have computed a new rating
            // at the same time.
            LocalPilotRanking = LocalPilotRanking.Merge(localPlayer.Ranking);

            // Note: Keep the UpToDate flags separate by merging separately both ways.
            localPlayer.Ranking = localPlayer.Ranking.Merge(LocalPilotRanking);

            if (!SteamLeaderboardService.IsDownloadingOrUploadingRanking &&
                LocalPilotRanking.IsValid &&
                LastKnownSteamRanking.RatingAwardedTime < LocalPilotRanking.RatingAwardedTime &&
                LocalPilotRanking.State == PilotRanking.StateType.RatingCalculated)
            {
                Log.Write($"LocalPilotRankingHandler: Uploading new rating {LastKnownSteamRanking} -> {LocalPilotRanking} to Steam.");
                UploadPilotRating();
            }

            if (!SteamLeaderboardService.IsDownloadingOrUploadingRanking && !LocalPilotRanking.IsRankValid && LastFetchAttempted.AddSeconds(360) < DateTime.UtcNow)
            {
                Log.Write($"LocalPilotRankingHandler: No valid pilot ranking. Attempting to fetch.");
                FetchLocalPlayerRanking();
            }

            if (!SteamLeaderboardService.IsDownloadingOrUploadingRanking && ScheduledRankingFetch < DateTime.UtcNow)
            {
                ScheduledRankingFetch = DateTime.MaxValue;
                Log.Write($"LocalPilotRankingHandler: Scheduled ranking fetch.");
                FetchLocalPlayerRanking();
            }
        }

        private void UploadPilotRating()
        {
            var uploadedRanking = LocalPilotRanking;
            var lastKnownSteamRanking = LastKnownSteamRanking;
            var uploadTime = DateTime.UtcNow;

            var uploadResult = SteamLeaderboardService.UploadCurrentPilotRanking(uploadedRanking, (updatedRanking, errors) =>
            {
                if (errors)
                {
                    Log.Write($"LocalPilotRankingHandler: Tried to upload rating, but error occured.");
                }
                else
                {

                    if (lastKnownSteamRanking == LastKnownSteamRanking)
                    {
                        LastKnownSteamRanking = LastKnownSteamRanking.Merge(updatedRanking).WithUpToDate(true);
                    }

                    if (LocalPilotRanking == uploadedRanking)
                    {
                        LocalPilotRanking = LocalPilotRanking.Merge(updatedRanking).WithUpToDate(true);
                        LastKnownSteamRanking = LocalPilotRanking;
                        Log.Write($"LocalPilotRankingHandler: Rating uploaded and rank updated: {uploadedRanking} -> {LocalPilotRanking}");
                    }
                    else
                    {
                        Log.Write("LocalPilotRankingHandler: Local ranking updated otherwise while uploading to Steam");
                    }

                    // Schedule a "fetch" around 3 seconds after rating was awarded to have a good chance of getting fresh ranking from Steam
                    // that includes the updates from other clients also.
                    ScheduledRankingFetch = DateTime.UtcNow.AddSeconds(3);
                }
            });
            if (!uploadResult)
            {
                Log.Write("LocalPilotRankingHandler: Rating upload not possible.");
            }
        }

        private void FetchLocalPlayerRanking()
        {
            if (!Game.IsSteam)
            {
                return;
            }

            if (SteamLeaderboardService.IsDownloadingOrUploadingRanking)
            {
                Log.Write($"LocalPilotRankingHandler: Skip fetch. Already uploading or getting rankings from Steam. Previous attempt {LastFetchAttempted.ToString("s", DateTimeFormatInfo.InvariantInfo)}");
                return;
            }
            else
            {
                Log.Write($"LocalPilotRankingHandler: Attempting to fetch local player ranking from Steam. Previous attempt {LastFetchAttempted.ToString("s", DateTimeFormatInfo.InvariantInfo)}");
                LastFetchAttempted = DateTime.UtcNow;
            }

            var localRankingOriginal = LocalPilotRanking;

            var fetchTime = DateTime.UtcNow;

            SteamLeaderboardService.GetCurrentPlayerRanking((SteamLeaderboardService.PlayerRankingResultDelegate)((ranking, errors) =>
            {
                if (errors)
                {
                    Log.Write("LocalPilotRankingHandler: Error getting Ranking from Steam");
                    // Attempt fetch again with increasing delay
                    fetchErrorCount++;
                    ScheduledRankingFetch = DateTime.UtcNow.AddSeconds(3 * fetchErrorCount);
                }
                else
                {
                    if (LocalPilotRanking == localRankingOriginal)
                    {
                        LocalPilotRanking = LocalPilotRanking.Merge(ranking).WithUpToDate(true);
                        LastKnownSteamRanking = LocalPilotRanking;
                        Log.Write($"LocalPilotRankingHandler: Fresh ranking update from Steam: {localRankingOriginal} -> {LocalPilotRanking}");
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
