using System.Linq;
using NUnit.Framework;

namespace AW2.Stats
{
    [TestFixture]
    public class MultiEloTest
    {
        private readonly double Delta = 0.0001;

        [Test]
        public void TestExpectedScore()
        {
            var multiElo = new MultiElo(k: 32, d: 400, logBase: 10);
            Assert.AreEqual(0.99009, multiElo.ExpectedScore(-800), Delta);
            Assert.AreEqual(0.90909, multiElo.ExpectedScore(-400), Delta);
            Assert.AreEqual(0.75974, multiElo.ExpectedScore(-200), Delta);
            Assert.AreEqual(0.64006, multiElo.ExpectedScore(-100), Delta);
            Assert.AreEqual(0.5, multiElo.ExpectedScore(0), Delta);
            Assert.AreEqual(0.49856, multiElo.ExpectedScore(1), Delta);
            Assert.AreEqual(0.49712, multiElo.ExpectedScore(2), Delta);
            Assert.AreEqual(0.35993, multiElo.ExpectedScore(100), Delta);
            Assert.AreEqual(0.24025, multiElo.ExpectedScore(200), Delta);
            Assert.AreEqual(0.09090, multiElo.ExpectedScore(400), Delta);
            Assert.AreEqual(0.00990, multiElo.ExpectedScore(800), Delta);

            var multiEloD1 = new MultiElo(k: 32, d: 1, logBase: 10);
            Assert.AreEqual(0.99999, multiEloD1.ExpectedScore(-10), Delta);
            Assert.AreEqual(0.90909, multiEloD1.ExpectedScore(-1), Delta);
            Assert.AreEqual(0.5, multiEloD1.ExpectedScore(0), Delta);
            Assert.AreEqual(0.24025, multiEloD1.ExpectedScore(0.5f), Delta);
            Assert.AreEqual(0.09090, multiEloD1.ExpectedScore(1), Delta);
            Assert.AreEqual(0.00990, multiEloD1.ExpectedScore(2), Delta);
            Assert.AreEqual(0, multiEloD1.ExpectedScore(10), Delta);
        }

        [Test]
        public void TestExpectedScores()
        {
            var multiElo = new MultiElo(k: 32, d: 400, logBase: 10);

            // Both players have same rating, but first one completely dominates
            var inputRatings = new[]
            {
                new EloRating<string>() { PlayerId = "A", Rating = 1000, Score = 100 },
                new EloRating<string>() { PlayerId = "B", Rating = 1000, Score = 0 },
            };

            var expectedScores = multiElo.ExpectedScores(inputRatings);

            Assert.That(expectedScores, Is.EqualTo(new[] { 0.5, 0.5 }).Within(Delta));
        }


        [Test]
        public void TestExpectedScores3()
        {
            var multiElo = new MultiElo(k: 32, d: 400, logBase: 10);

            // Both players have same rating, but first one completely dominates
            var inputRatings = new[]
            {
                new EloRating<string>() { PlayerId = "A", Rating = 1200, Score = 0 },
                new EloRating<string>() { PlayerId = "B", Rating = 1000, Score = 0 },
                new EloRating<string>() { PlayerId = "C", Rating = 900, Score = 0 },
            };

            var expectedScores = multiElo.ExpectedScores(inputRatings);

            Assert.That(expectedScores, Is.EqualTo(new[] { 0.5362, 0.2934, 0.1703 }).Within(Delta));
        }

        [Test]
        public void TestEvenRatingsTotalWin()
        {
            var multiElo = new MultiElo(k: 32, d: 400, logBase: 10);

            // Both players have same rating, but first one completely dominates
            var inputRatings = new[]
            {
                new EloRating<string>() { PlayerId = "A", Rating = 1000, Score = 100 },
                new EloRating<string>() { PlayerId = "B", Rating = 1000, Score = 0 },
            };

            var updated = multiElo.Update(inputRatings);

            Assert.AreEqual(updated.Select(r => r.Rating), new[] { 1016, 984 });
        }

        [Test]
        public void TestZeroRatingsNoNegativeResult()
        {
            var multiElo = new MultiElo(k: 32, d: 400, logBase: 10);

            // Both players have same rating, but first one completely dominates
            var inputRatings = new[]
            {
                new EloRating<string>() { PlayerId = "A", Rating = 0, Score = 100 },
                new EloRating<string>() { PlayerId = "B", Rating = 0, Score = 0 },
            };

            var updated = multiElo.Update(inputRatings);

            Assert.AreEqual(updated.Select(r => r.Rating), new[] { 16, 0 });
        }

        [Test]
        public void Test3Player()
        {
            var multiElo = new MultiElo(k: 32, d: 400, logBase: 10);

            // Both players have same rating, but first one completely dominates
            var inputRatings = new[]
            {
                new EloRating<string>() { PlayerId = "A", Rating = 1200, Score = 6666 },
                new EloRating<string>() { PlayerId = "B", Rating = 1000, Score = 3333 },
                new EloRating<string>() { PlayerId = "B", Rating = 900, Score = 0 },
            };

            var updated = multiElo.Update(inputRatings);

            Assert.AreEqual(updated.Select(r => r.Rating), new[] { 1208, 1003, 889 });
        }

        [Test]
        public void TestEvenRatingsTie()
        {
            var multiElo = new MultiElo(k: 32, d: 400, logBase: 1);

            // Both players have same rating and same score
            var inputRatings = new[]
            {
                new EloRating<string>() { PlayerId = "A", Rating = 1000, Score = 100 },
                new EloRating<string>() { PlayerId = "B", Rating = 1000, Score = 100 },
            };

            var updated = multiElo.Update(inputRatings);

            Assert.AreEqual(updated.Select(r => r.Rating), new[] { 1000, 1000 });
        }

        [Test]
        public void TestEvenRatingsWin()
        {
            var multiElo = new MultiElo(k: 32, d: 400, logBase: 10);

            // Both players have same rating and same score
            var inputRatings = new[]
            {
                new EloRating<string>() { PlayerId = "A", Rating = 1000, Score = 50 },
                new EloRating<string>() { PlayerId = "B", Rating = 1000, Score = 100 },
            };

            var updated = multiElo.Update(inputRatings);

            Assert.AreEqual(updated.Select(r => r.Rating), new[] { 995, 1005 });
        }
    }
}
