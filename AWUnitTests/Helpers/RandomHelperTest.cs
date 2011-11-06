using System;
using NUnit.Framework;

namespace AW2.Helpers
{
    [TestFixture]
    public class RandomHelperTest
    {
        [Test]
        public void TestGetRandomInt()
        {
            TestEvenDistributionInt(4096, 1000, () => RandomHelper.GetRandomInt());
        }

        [Test]
        public void TestShiftRandomInt()
        {
            int seed = RandomHelper.GetRandomInt();
            TestEvenDistributionInt(8192, 1000, () => seed = RandomHelper.ShiftRandomInt(seed));
            TestPredictability(seed, 1000, (x, n) => RandomHelper.ShiftRandomInt(x));
        }

        [Test]
        public void TestMixRandomInt()
        {
            int seed = RandomHelper.GetRandomInt();
            TestPredictability(seed, 1000, (x, k) => RandomHelper.MixRandomInt(seed, k));
        }

#if POISSON_DISTRIBUTION_IMPLEMENTED
        [Test]
        public void TestGetRandomIntPoisson()
        {
            /*
            * Universal generator as described in "Non-Uniform Random Variate Generation"
            * by Luc Devroye (McGill University).
            L = mean
            m = mode = Math.Floor(L) (unless mode is integer, in which case mode -= 0.5)
            p(k) = probability of case k = L^k * e^-L / k!

            Compute w = 1 + p(m)/2
            repeat
            Generate U, V, W uniformly on [0, 1], and let S be a random sign.
            if U <= w/(1+w)
            then Y = V * w/p(m)
            else Y = (w - log(V))/p(m)
            X = S * round(Y)
            until W * min(1, e^(w - p(m) * Y) <= p(m+X)/p(m)
            return m + X
            */
        }
#endif // POISSON_DISTRIBUTION_IMPLEMENTED

        /// <summary>
        /// Helper method. Tests for even distribution of ints given by an
        /// int randomising delegate.
        /// </summary>
        /// <param name="intervalCount">Number of intervals in which random numbers
        /// are grouped for checking. Must be a power of two. Higher values give 
        /// higher confidence in the check but require a higher <c>averageCount</c>.</param>
        /// <param name="averageCount">Average number of random numbers to expect
        /// in one interval. Must be "large enough" for the check to be reliable.
        /// Larger numbers imply more computation.</param>
        /// <param name="randomizer">Function returning random ints.</param>
        void TestEvenDistributionInt(int intervalCount, int averageCount, Func<int> randomizer)
        {
            int intervalLength = (int)(65536.0 * 65536.0 / intervalCount); // must use floating point numbers to express the number of 32-bit integers
            var counts = new int[intervalCount]; // counts of random numbers grouped into intervals
            for (int i = 0; i < intervalCount * averageCount; ++i)
            {
                int value = randomizer();
                int interval = (int)(((long)value - int.MinValue) / intervalLength);
                ++counts[interval];
            }

            // Expect a count of 'multipleCount' in each interval.
            int epsilon = 0;
            int worstInterval = -1;
            for (int i = 0; i < intervalCount; ++i)
                if (Math.Abs(averageCount - counts[i]) > epsilon)
                {
                    epsilon = Math.Abs(averageCount - counts[i]);
                    worstInterval = i;
                }
            int[] showIntervals = 
            { 
                0, 1, 2, 
                intervalCount / 2 - 1, intervalCount / 2, intervalCount / 2 + 1,
                intervalCount - 3, intervalCount - 2, intervalCount - 1
            };
            Assert.Less(epsilon, (int)(averageCount * 0.16),
                "Random distribution doesn't look very even. Expected average " + averageCount + " hits for each interval."
                + "\nWorst interval is " + worstInterval + ": " + counts[worstInterval]
                + "\nSome more intervals:"
                + string.Concat(Array.ConvertAll<int, string>(showIntervals, (i) => "\n  " + i + ": " + counts[i]))
                + "\n*** Please rerun the test several times and worry only if it fails repeatedly ***");
        }

        /// <summary>
        /// Tests the predictability of a random sequence.
        /// </summary>
        /// <param name="seed">Start seed.</param>
        /// <param name="runLength">Length of test run.</param>
        /// <param name="successor">Successor function, computing the next random
        /// number, given the previous random number and the index of the 
        /// number to produce.</param>
        void TestPredictability(int seed, int runLength, Func<int, int, int> successor)
        {
            var run1 = new int[runLength];
            var run2 = new int[runLength];
            run1[0] = run2[0] = seed;
            for (int i = 1; i < runLength; ++i)
                run1[i] = successor(run1[i - 1], i);
            for (int i = 1; i < runLength; ++i)
                run2[i] = successor(run2[i - 1], i);
            Assert.AreEqual(run1, run2, "Random is not predictable");
        }

#if POISSON_DISTRIBUTION_IMPLEMENTED
        /// <summary>
        /// Helper method. Tests for Poisson distribution of ints given by an
        /// int randomising delegate.
        /// </summary>
        /// <param name="randomizer">Random number generator to test.</param>
        /// <param name="mode">Mode of the Poisson distribution</param>
        void TestPoissonDistributionInt(Func<int> randomizer, int mode)
        {
            int[] counts = new int[2 * mode + 1]; // counts for 2*mode first numbers and a count for all the rest
            int totalCount = 10000000;
            for (int i = 0; i < totalCount; ++i)
            {
                int value = randomizer();
                int interval = value < counts.Length - 1 ? value : counts.Length;
                ++counts[interval];
            }

            float[] weights = new float[counts.Length];
            int epsilon = 0;
            int worstInterval = -1;
            for (int i = 0; i < counts.Length; ++i)
                weights[i] = counts[i] / (float)totalCount;
                //if (Math.Abs(1000 - counts[i]) > epsilon)
                //{
                //    epsilon = Math.Abs(1000 - counts[i]);
                //    worstInterval = i;
                //}
            Assert.Less(epsilon, 160, "Random distribution doesn't look very even, worst interval " + worstInterval
                + "\nsome intervals: 0=" + counts[0] + ", 1=" + counts[1] + ", 2=" + counts[2]
                + "\n32766=" + counts[32766] + ", 32767=" + counts[32767] + ", 32768=" + counts[32768]
                + "\n65533=" + counts[65533] + ", 65534=" + counts[65534] + ", 65535=" + counts[65535]
                + "\n*** Please rerun the test several times and worry only if it fails repeatedly ***");
        }
#endif // POISSON_DISTRIBUTION_IMPLEMENTED
    }
}
