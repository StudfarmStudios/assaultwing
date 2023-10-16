
using System.Globalization;
using AW2.Helpers;

namespace AW2.Stats
{
    public struct PilotRanking : IEquatable<PilotRanking>
    {
        /// <summary> The score that defines where in the ranking the player is. </summary>
        /// <remarks>0 means that the user has no score yet</remarks>
        public int Rating;

        // https://partner.steamgames.com/doc/api/ISteamUserStats#LeaderboardScoreUploaded_t
        /// <summary>Up to date rank is obtained from Steam when leaderboard entries are downloaded. </summary>
        /// <remarks>0 means that the user has no global leaderboard entry yet.</remarks>        
        public int Rank;

        /// <summary>
        /// Time when the rating score was awarded. Stored in the Steam leaderboard "UGC" extra data.
        /// Also used to track when the score needs to be uploaded to the Steam leaderboard 
        /// by the local player client.
        /// </summary>
        public DateTime RatingAwardedTime { get; set; }

        static public PilotRanking FromLeaderboardEntry(LeaderboardEntryAndUgc entryAndUgc)
        {
            var awardedTime = DateTime.MinValue;
            if (entryAndUgc.ugc.Length == 2)
            {
                awardedTime = new DateTime(((long)entryAndUgc.ugc[0] << 32) | (uint)entryAndUgc.ugc[1]);
            }
            return new PilotRanking
            {
                Rating = entryAndUgc.entry.m_nScore,
                Rank = entryAndUgc.entry.m_nGlobalRank,
                RatingAwardedTime = awardedTime,
            };
        }

        public PilotRanking UpdateRating(int rating, DateTime now)
        {
            if (rating == Rating) return this;
            return new PilotRanking
            {
                Rating = rating,
                // note that the rank is not accurate at this point, but it is still better than nothing.
                Rank = Rank,
                RatingAwardedTime = now,
            };
        }

        public List<int> SteamLeaderboardUGC
        {
            get
            {
                return new List<int> { (int)(RatingAwardedTime.Ticks >> 32), (int)(RatingAwardedTime.Ticks & 0xFFFFFFFF) };
            }
        }

        private static readonly DateTime AwardedTimeSanityCheckLow = DateTime.Parse("2023-07-01T00:00:00Z");

        public bool IsValid { get { return Rating > 0 && RatingAwardedTime > AwardedTimeSanityCheckLow; } }

        public string RankString
        {
            get
            {
                if (IsRankValid)
                {
                    return Rank.ToOrdinalString();
                }
                else
                {
                    return "n/a";
                }
            }
        }

        public bool IsRankValid => IsValid && Rank > 0; // Default value of 0 before we have downloaded a value back from the Steam leaderboard.

        public override string ToString()
        {
            var playerRating = string.Format(CultureInfo.InvariantCulture, "rating {0}", Rating.ToString());
            var awardedTime = RatingAwardedTime.ToString("s", DateTimeFormatInfo.InvariantInfo);
            return $"rank:{RankString} rating:{playerRating} awarded:{awardedTime}";
        }

        public bool Equals(PilotRanking other)
        {
            return Rating == other.Rating && Rank == other.Rank && RatingAwardedTime == other.RatingAwardedTime;
        }

        public static bool operator ==(PilotRanking left, PilotRanking right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PilotRanking left, PilotRanking right)
        {
            return !(left == right);
        }

        public override bool Equals(object? obj)
        {
            return obj is PilotRanking other && Equals(other);
        }

        public override int GetHashCode()
        {
            var hashCode = Rating;
            hashCode = (hashCode * 397) ^ Rank;
            hashCode = (hashCode * 397) ^ RatingAwardedTime.GetHashCode();
            return hashCode;
        }
    }
}
