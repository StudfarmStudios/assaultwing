
using System.Globalization;
using AW2.Helpers;

namespace AW2.Stats
{
    public struct PilotRanking : IEquatable<PilotRanking>
    {

        public enum StateType
        {
            /// <summary>
            /// Not initialized yet. This is the default value.
            /// </summary>
            NotInitialized,
            /// <summary>
            /// Rating and rank was attempted to be fetched from Steam, but there was
            /// some error and therefore the rating should not be updated for this player
            /// and default rating value should be assumed for current round.
            /// </summary>
            DownloadFailed,
            /// <summary>
            /// Rating was attempted to be downloaded, but there was no rating
            /// for this player. Therefore next rating calculated will be the
            /// first one for this player.
            /// </summary>
            NotRatedYet,
            /// <summary>
            /// This rating is downloaded from Steam.
            /// <summary>
            RankingDownloaded,
            /// <summary>
            /// A new rating was recently calculated by the server. Rank may
            /// not be up to date yet until the next leaderboard upload / download.
            /// <summary>
            RatingCalculated,
        }

        public StateType State { get; init; }

        /// <summary> The score that defines where in the ranking the player is. </summary>
        public int Rating { get; init; }

        // https://partner.steamgames.com/doc/api/ISteamUserStats#LeaderboardScoreUploaded_t
        /// <summary>Up to date rank is obtained from Steam when leaderboard entries are downloaded. </summary>
        public int Rank { get; init; }

        /// <summary>
        /// Time when the rating score was awarded. Stored in the Steam leaderboard "UGC" extra data.
        /// Also used to track when the score needs to be uploaded to the Steam leaderboard 
        /// by the local player client.
        /// </summary>
        public DateTime RatingAwardedTime { get; init; }

        /// <summary>
        /// Time when the Rank was downloaded from Steam. Used to ensure that newer rank
        /// information obtained by the client for its own user is not overwritten with
        /// older data from the server.
        /// </summary>
        public DateTime RankDownloadedTime { get; init; }

        public bool UpToDate { get; init; }

        public PilotRanking WithUpToDate(bool upToDate) =>
            new PilotRanking() { State = State, Rank = Rank, RankDownloadedTime = RankDownloadedTime, Rating = Rating, RatingAwardedTime = RatingAwardedTime, UpToDate = upToDate };

        static public PilotRanking FromLeaderboardEntry(LeaderboardEntryAndUgc entryAndUgc)
        {
            var awardedTime = DateTime.MinValue;
            if (entryAndUgc.ugc.Length == 2)
            {
                awardedTime = new DateTime(((long)entryAndUgc.ugc[0] << 32) | (uint)entryAndUgc.ugc[1]);
            }

            return new PilotRanking
            {
                State = StateType.RankingDownloaded,
                Rating = entryAndUgc.entry.m_nScore,
                Rank = entryAndUgc.entry.m_nGlobalRank,
                RatingAwardedTime = awardedTime,
                RankDownloadedTime = DateTime.Now,
            };
        }

        public List<int> SteamLeaderboardUGC
        {
            get
            {
                return new List<int> { (int)(RatingAwardedTime.Ticks >> 32), (int)(RatingAwardedTime.Ticks & 0xFFFFFFFF) };
            }
        }

        private static readonly DateTime TimeSanityCheckLow = DateTime.Parse("2023-07-01T00:00:00Z");

        public bool IsStateValid
        {
            get
            {
                return State switch
                {
                    StateType.DownloadFailed or
                    StateType.NotRatedYet or
                    StateType.RankingDownloaded or
                    StateType.RatingCalculated
                      => true,
                    _ => false,
                };
            }
        }

        public bool IsValid
        {
            get
            {
                return IsStateValid &&
                    Rating >= 0 &&
                    RatingAwardedTime >= TimeSanityCheckLow &&
                    ((Rank == 0 && RankDownloadedTime == DateTime.MinValue) ||
                     (Rank > 0 && RankDownloadedTime >= TimeSanityCheckLow));
            }
        }

        public string RankString
        {
            get => IsRankValid ? Rank.ToOrdinalString() : "n/a";
        }

        public string RatingString
        {
            get => IsValid ? Rating.ToString() : "n/a";
        }

        public bool IsRankValid => IsValid && Rank > 0; // Default value of 0 before we have downloaded a value back from the Steam leaderboard.

        public override string ToString()
        {
            var rating = IsValid ? Rating + "/" + RatingAwardedTime.ToString("s", DateTimeFormatInfo.InvariantInfo) : "n/a";
            var rank = IsRankValid ? RankString + "/" + RankDownloadedTime.ToString("s", DateTimeFormatInfo.InvariantInfo) : "n/a";
            return $"[{State} rank:{rank} rating:{rating} UpToDate:{UpToDate}]";
        }

        public bool Equals(PilotRanking other)
        {
            return
            State == other.State &&
            Rating == other.Rating &&
            Rank == other.Rank &&
            RatingAwardedTime == other.RatingAwardedTime &&
            RankDownloadedTime == other.RankDownloadedTime &&
            UpToDate == other.UpToDate;
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
            var hashCode = (int)State;
            hashCode = (hashCode * 397) ^ Rank;
            hashCode = (hashCode * 397) ^ Rating;
            hashCode = (hashCode * 397) ^ RankDownloadedTime.GetHashCode();
            hashCode = (hashCode * 397) ^ RatingAwardedTime.GetHashCode();
            hashCode = (hashCode * 397) ^ UpToDate.GetHashCode();
            return hashCode;
        }

        /// <summary>
        /// A method that is used by the syncing protocol that uses the Server to calculate
        /// new ratings for all clients based on previous ratings they have downloaded for
        /// them selves from steam, and then distributes updated ratings for all clients.
        /// 
        /// This method is used when moving data around to ensure each part ends up with
        /// the latest possible rating and ranking data.
        /// </summary>
        public readonly PilotRanking Merge(PilotRanking possiblyNew)
        {
            var state = State;
            var rank = Rank;
            var rankDownloadedTime = RankDownloadedTime;
            var rating = Rating;
            var ratingAwardedTime = RatingAwardedTime;

            if (possiblyNew.State == StateType.RankingDownloaded ||
                 possiblyNew.State == StateType.RatingCalculated)
            {
                // Accept new rank / rating only if it is recently calculated by server or downloaded from Steam.

                if (possiblyNew.IsRankValid &&
                    RankDownloadedTime < possiblyNew.RankDownloadedTime)
                {
                    state = possiblyNew.State;
                    rank = possiblyNew.Rank; // We have no Rank and the other one has, so we take it.
                    rankDownloadedTime = possiblyNew.RankDownloadedTime;
                }

                if (possiblyNew.IsValid &&
                    RatingAwardedTime < possiblyNew.RatingAwardedTime)
                {
                    state = possiblyNew.State;
                    ratingAwardedTime = possiblyNew.RatingAwardedTime;
                    rating = possiblyNew.Rating;
                }
            }
            else
            {
                // Record the most interesting state
                if (possiblyNew.State > State)
                {
                    state = possiblyNew.State;
                }
            }

            var result = new PilotRanking
            {
                State = state,
                Rank = rank,
                RankDownloadedTime = rankDownloadedTime,
                Rating = rating,
                RatingAwardedTime = ratingAwardedTime,
                UpToDate = UpToDate,
            };

            return result.WithUpToDate(result == this && UpToDate);
        }
    }
}
