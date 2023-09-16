using Steamworks;
using AW2.Helpers;
using AW2.Core;

namespace AW2.Stats
{
    /// Game server API does not seem to have leaderboards:
    /// https://partner.steamgames.com/doc/api/ISteamGameServerStats
    ///
    /// https://partner.steamgames.com/doc/api/ISteamUserStats#LeaderboardFindResult_t
    /// https://steamworks.github.io/gettingstarted/#steam-callresults
    public class SteamLeaderboardService : IDisposable
    {

        public delegate void RankingUploadDelegateType(PilotRanking updatedRanking, bool errors);

        public delegate void LeaderboardResultDelegate(LeaderboardEntryAndUgc[] entries, bool errors);

        public delegate void RankingsResultDelegate(Dictionary<CSteamID, PilotRanking> entries, bool errors);

        public delegate void PlayerRankingResultDelegate(PilotRanking ranking, bool errors);

        private static readonly SteamLeaderboard_t EmptyLeaderboard;
        private SteamApiService SteamApiService;
        private SteamLeaderboard_t PilotRankingLeaderboard;

        private CallResult<LeaderboardFindResult_t>? FindLeaderBoardCallResult;

        private CallResult<LeaderboardScoresDownloaded_t>? DownloadLeaderboardEntriesCallResult;
        private CallResult<LeaderboardScoreUploaded_t>? LeaderboardUpdateResult;

        private LeaderboardResultDelegate? DownloadLeaderboardEntriesResultDelegate;

        private RankingUploadDelegateType? RankingUploadDelegate;

        public PilotRanking UploadedRanking { get; private set; }

        public SteamLeaderboardService(SteamApiService steamApiService)
        {
            SteamApiService = steamApiService;
            if (SteamApiService.Initialized)
            {
                Initialize();
            }
        }

        private static readonly String RankingLeaderboardName = "pilot_ranking";

        private void Initialize()
        {
            FindLeaderBoardCallResult = CallResult<LeaderboardFindResult_t>.Create(LeaderBoardFindResult);
            var steamApiCall = SteamUserStats.FindLeaderboard(RankingLeaderboardName);
            FindLeaderBoardCallResult.Set(steamApiCall);
        }

        private void LeaderBoardFindResult(LeaderboardFindResult_t param, bool ioFailure)
        {
            if (param.m_bLeaderboardFound == 0 || ioFailure)
            {
                Log.Write($"Failed to find Steam Leaderboard {RankingLeaderboardName} found:{param.m_bLeaderboardFound} ioFailure:{ioFailure}");
            }
            else
            {
                Log.Write($"Found Steam Leaderboard {RankingLeaderboardName}");
                PilotRankingLeaderboard = param.m_hSteamLeaderboard;
            }
        }

        private void LeaderBoardScoreUploaded(LeaderboardScoreUploaded_t uploaded, bool ioFailure)
        {
            var delegateToCall = RankingUploadDelegate;

            if (delegateToCall is not null && UploadedRanking.IsValid)
            {
                var updatedRanking = UploadedRanking;
                RankingUploadDelegate = null;
                UploadedRanking = new PilotRanking();
                updatedRanking.Rank = uploaded.m_nGlobalRankNew;
                delegateToCall(updatedRanking, errors: ioFailure);
            }

            LeaderboardUpdateResult?.Dispose();
            LeaderboardUpdateResult = null;
        }

        public bool IsUploadingRanking => RankingUploadDelegate is not null;

        private const int MaxLeaderboardDetails = 2; // currently timestamp is stored in the extra data

        private void DownloadedLeaderBoardEntriesResult(LeaderboardScoresDownloaded_t downloaded, bool ioFailure)
        {
            LeaderboardEntryAndUgc[] leaderboardEntries = new LeaderboardEntryAndUgc[downloaded.m_cEntryCount];
            int[] ugcForOneEntry = new int[MaxLeaderboardDetails];
            for (int index = 0; index < downloaded.m_cEntryCount; index++)
            {
                LeaderboardEntry_t entry;
                SteamUserStats.GetDownloadedLeaderboardEntry(downloaded.m_hSteamLeaderboardEntries, index, out entry, ugcForOneEntry, MaxLeaderboardDetails);
                leaderboardEntries[index].entry = entry;
                leaderboardEntries[index].ugc = ugcForOneEntry;
            }

            if (DownloadLeaderboardEntriesResultDelegate != null)
            {
                DownloadLeaderboardEntriesResultDelegate(leaderboardEntries, errors: ioFailure);
                DownloadLeaderboardEntriesResultDelegate = null;
            }

            DownloadLeaderboardEntriesCallResult?.Dispose();
            DownloadLeaderboardEntriesCallResult = null;
        }

        /// <summary>
        /// Note that this method can't called multiple times in parallel or in parallel with GetCurrentPlayerRanking.
        /// </summary>
        public void GetRankings(List<CSteamID> steamIds, RankingsResultDelegate resultDelegate)
        {
            if (PilotRankingLeaderboard != EmptyLeaderboard)
            {
                DownloadLeaderboard(PilotRankingLeaderboard, steamIds, (entries, errors) =>
                {
                    var rankingsDictionary = new Dictionary<CSteamID, PilotRanking>(
                        entries
                        .Select(entry => new KeyValuePair<CSteamID, PilotRanking>(
                            entry.entry.m_steamIDUser, PilotRanking.FromLeaderboardEntry(entry))));

                    resultDelegate(rankingsDictionary, errors: errors);
                });
            }
        }

        /// <summary>
        /// Note that this method can't called multiple times in parallel or in parallel with GetRankings.
        /// </summary>
        public void GetCurrentPlayerRanking(PlayerRankingResultDelegate resultDelegate)
        {
            if (PilotRankingLeaderboard != EmptyLeaderboard)
            {
                var call = SteamUserStats.DownloadLeaderboardEntries(PilotRankingLeaderboard, ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobalAroundUser, 0, 0);
                DownloadLeaderboardEntriesResultDelegate = (entries, errors) =>
                {
                    if (entries.Count() == 0)
                    {
                        resultDelegate(new PilotRanking(), errors: true);
                    }
                    else
                    {
                        var ranking = PilotRanking.FromLeaderboardEntry(entries.First());
                        resultDelegate(ranking, errors: errors);
                    }
                };
                DownloadLeaderboardEntriesCallResult = new CallResult<LeaderboardScoresDownloaded_t>(DownloadedLeaderBoardEntriesResult);
                DownloadLeaderboardEntriesCallResult?.Set(call, DownloadedLeaderBoardEntriesResult);
            }
        }

        private void DownloadLeaderboard(SteamLeaderboard_t leaderboard, List<CSteamID> steamIds, LeaderboardResultDelegate resultDelegate)
        {
            var call = SteamUserStats.DownloadLeaderboardEntriesForUsers(
                leaderboard,
                steamIds.ToArray(),
                steamIds.Count
            );

            DownloadLeaderboardEntriesResultDelegate = resultDelegate;
            DownloadLeaderboardEntriesCallResult = new CallResult<LeaderboardScoresDownloaded_t>(DownloadedLeaderBoardEntriesResult);
            DownloadLeaderboardEntriesCallResult?.Set(call, DownloadedLeaderBoardEntriesResult);
        }

        public bool IsGettingRankings
        {
            get
            {
                return DownloadLeaderboardEntriesResultDelegate != null;
            }
        }

        // https://partner.steamgames.com/doc/api/ISteamUserStats#UploadLeaderboardScore
        // https://partner.steamgames.com/doc/features/leaderboards/guide
        // Each client must upload their own score. 
        // This needs to be coordinated with the server assuming the server computes the scores at the end
        // of the round for all players.
        // Maybe later we will implemented the trusted
        // version of the leaderboard so only verified servers can upload scores.
        // There is a rate limit of 10 updates per 10 minutes.
        public bool UploadCurrentPilotRanking(PilotRanking ranking, RankingUploadDelegateType resultDelegate)
        {
            if (PilotRankingLeaderboard != EmptyLeaderboard && LeaderboardUpdateResult is null)
            {
                var extraData = ranking.SteamLeaderboardUGC;
                var result = SteamUserStats.UploadLeaderboardScore(
                    PilotRankingLeaderboard,
                    // Force update because the scores can also decrease according to Elo ranking.
                    ELeaderboardUploadScoreMethod.k_ELeaderboardUploadScoreMethodForceUpdate,
                    ranking.Rating,
                    extraData.ToArray(),
                    extraData.Count
                );
                RankingUploadDelegate = resultDelegate;
                UploadedRanking = ranking;
                LeaderboardUpdateResult = CallResult<LeaderboardScoreUploaded_t>.Create(LeaderBoardScoreUploaded);
                LeaderboardUpdateResult.Set(result);
                return true;
            }
            else
            {
                return false;
            }
        }

        public void Dispose()
        {
            FindLeaderBoardCallResult?.Dispose();
            FindLeaderBoardCallResult = null;
            DownloadLeaderboardEntriesResultDelegate = null;

            LeaderboardUpdateResult?.Dispose();
            LeaderboardUpdateResult = null;

            DownloadLeaderboardEntriesCallResult?.Dispose();
            DownloadLeaderboardEntriesCallResult = null;
        }
    }
}
