using Steamworks;

namespace AW2.Stats
{
    /// <summary>
    /// Just the Steam base leaderboard entry and the "user generated data" UGC in one struct.
    /// </summary>
    public struct LeaderboardEntryAndUgc
    {
        public LeaderboardEntry_t entry;
        public int[] ugc;
    }
}
