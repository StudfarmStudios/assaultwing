using AW2.Core;
using AW2.Helpers;

namespace AW2.Stats
{

    /// <summary>
    /// Runs on client and check to see if local player's PilotRanking needs to be uploaded to steam leaderboard
    /// </summary>
    public class PilotRatingUploader : AWGameComponent
    {

        private SteamLeaderboardService SteamLeaderboardService => Game.Services.GetService<SteamLeaderboardService>();

        /// <summary>
        /// Used on client to check if the local client's ranking needs to be uploaded to Steam now.
        /// </summary>
        private PilotRanking LocalClientLastUploadedRanking;

        public PilotRatingUploader(AssaultWingCore game) : base(game, 100)
        {
        }

        /// <summary>
        /// Checks if local player has been awarded a new rating and if so, uploads it to the Steam leaderboard.
        /// </summary>
        override public void Update()
        {
            var localPlayer = Game.DataEngine.LocalPlayer;

            if (localPlayer is null)
            {
                return; // Player does not exist while we are in the start menu.
            }

            var localRanking = localPlayer.Ranking;

            if (localRanking.IsValid &&
               localRanking.RatingAwardedTime > LocalClientLastUploadedRanking.RatingAwardedTime)
            {
                // If the upload fails, don't retry. Next round will try again.
                LocalClientLastUploadedRanking = localRanking;
                if (SteamLeaderboardService.UploadCurrentPilotRanking(localRanking))
                {
                    Log.Write($"PilotRatingUploader: Uploading ranking {localRanking}");
                }
                else
                {
                    Log.Write($"PilotRatingUploader: Rating upload not possible. {localRanking}");
                }
            }
        }
    }
}
