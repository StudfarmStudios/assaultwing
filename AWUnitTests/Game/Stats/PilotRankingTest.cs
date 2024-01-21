using NUnit.Framework;
using System;

namespace AW2.Stats
{
    [TestFixture]
    public class PilotRankingTest
    {
        private readonly PilotRanking NotInitialized = new PilotRanking() { State = PilotRanking.StateType.NotInitialized };

        private readonly PilotRanking DownloadFailed = new PilotRanking() { State = PilotRanking.StateType.DownloadFailed };

        private readonly PilotRanking NotRatedYet = new PilotRanking() { State = PilotRanking.StateType.NotRatedYet };

        private readonly PilotRanking FirstTimeRated = new PilotRanking()
        {
            State = PilotRanking.StateType.RatingCalculated,
            Rating = 456,
            RatingAwardedTime = DateTime.Parse("2023-12-01T00:00:00Z")
        };

        private readonly PilotRanking Downloaded = new PilotRanking()
        {
            State = PilotRanking.StateType.RankingDownloaded,
            Rating = 123,
            RatingAwardedTime = DateTime.Parse("2023-12-02T00:00:00Z"),
            Rank = 3,
            RankDownloadedTime = DateTime.Parse("2023-12-03T00:00:00Z")
        };

        private readonly PilotRanking RatedAgain = new PilotRanking()
        {
            State = PilotRanking.StateType.RankingDownloaded,
            Rating = 456,
            RatingAwardedTime = DateTime.Parse("2023-12-03T00:00:00Z"),
            Rank = 3,
            RankDownloadedTime = DateTime.Parse("2023-12-03T00:00:00Z"),
        };

        [Test]
        public void TestMerge()
        {
            Assert.AreEqual(NotInitialized, NotInitialized.Merge(NotInitialized));

            Assert.AreEqual(DownloadFailed, DownloadFailed.Merge(NotInitialized));

            Assert.AreEqual(Downloaded, NotInitialized.Merge(Downloaded));

            Assert.AreEqual(Downloaded, Downloaded.Merge(DownloadFailed));

            Assert.AreEqual(Downloaded, NotRatedYet.Merge(Downloaded));

            Assert.AreEqual(FirstTimeRated, FirstTimeRated.Merge(NotRatedYet));

            Assert.AreEqual(FirstTimeRated, NotRatedYet.Merge(FirstTimeRated));

            Assert.AreEqual(RatedAgain, RatedAgain.Merge(FirstTimeRated));

            Assert.AreEqual(RatedAgain, FirstTimeRated.Merge(RatedAgain));

            Assert.AreEqual(RatedAgain, Downloaded.Merge(RatedAgain));
        }
    }
}
