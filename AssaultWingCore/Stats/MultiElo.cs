
namespace AW2.Stats
{
    /// Implemented based on this article
    /// https://towardsdatascience.com/developing-a-generalized-elo-rating-system-for-multiplayer-games-b9b495e87802
    public class MultiElo
    {
        readonly float K;
        readonly float D;
        readonly float LogBase;

        /// <param name="k">The parameter of how much ratings are updated.
        /// Typically for example 32.</param>
        ///
        /// <param name="d">The parameter controlling how difference in ratings
        /// translates to expected winning probabilities. Typically for example
        /// 400. Larger values translate the rating more weakly to probability
        /// of winning. Affects the method <c>ExpectedScores</c>.</param>
        ///
        /// <param name="logBase">The parameter controlling the exponential
        /// distribution of the "expected scores". Often used value is 10.
        /// Affects the method <c>ExpectedScore</c>.</param>
        public MultiElo(float k, float d, float logBase)
        {
            K = k;
            D = d;
            LogBase = logBase;
        }

        public EloRating<ID>[] Update<ID>(EloRating<ID>[] ratings)
        {
            int n = ratings.Count();
            EloRating<ID>[] results = new EloRating<ID>[n];
            var expectedScores = ExpectedScores(ratings);

            double scoreSum = 0;
            for (int i = 0; i < n; i++)
            {
                if (ratings[i].Score > 0) // treat negative scores as 0
                {
                    scoreSum += ratings[i].Score;
                }
            }

            for (int i = 0; i < n; i++)
            {
                var prevRating = ratings[i];

                double normalizedScore = 0;

                if (prevRating.Score > 0 && scoreSum > 0) // treat negative scores as 0
                {
                    normalizedScore = (prevRating.Score) / scoreSum;
                }

                double updatedRatingValue = prevRating.Rating;
                if (scoreSum > 0) // If the sum is zero, don't update ratings
                {
                    updatedRatingValue = prevRating.Rating + K * (n - 1) * (normalizedScore - expectedScores[i]);
                }

                results[i] = new EloRating<ID>
                {
                    PlayerId = prevRating.PlayerId,
                    Score = 0,
                    Rating = (int)Math.Round(updatedRatingValue)
                };
            }

            return results;
        }

        public double ExpectedScore(float scoreDiff)
        {
            return 1.0 / (1.0 + Math.Pow(LogBase, scoreDiff / D));
        }

        public double[] ExpectedScores<ID>(EloRating<ID>[] ratings)
        {
            int n = ratings.Count();
            double normalization = n * (n - 1) / 2;
            var result = new double[n];
            for (int i = 0; i < n; i++)
            {
                double sum = 0;
                for (int j = 0; j < n; j++)
                {
                    if (i != j)
                    {
                        var diff = ratings[j].Rating - ratings[i].Rating;
                        var score = ExpectedScore(diff);
                        sum += score;
                    }
                }
                result[i] = sum / normalization;
            }

            return result;
        }
    }

    public struct EloRating<ID>
    {
        /// External ID for the player
        public readonly ID PlayerId { get; init; }
        /// The (Multi) Elo Rating of this player. Either previous value or
        /// updated according to MatchPosition.
        public readonly int Rating { get; init; }
        /// The score of this player for the previous round. The scores are
        /// internally normalized so that they sum to one. Tied scores are OK.
        public readonly int Score { get; init; }
    }
}
