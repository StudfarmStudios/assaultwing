using System;
using System.Linq;
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

        [Test]
        public void TestShuffle()
        {
            AssertShuffle(new int[0]);
            AssertShuffle(new[] { 1, 2, 3, 4, 5 });
            AssertShuffle(new[] { 1, 1 });
        }

        private void AssertShuffle(int[] original)
        {
            var shuffled = original.Shuffle();
            Assert.AreEqual(original, shuffled.OrderBy(x => x).ToArray());
        }

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
        private void TestEvenDistributionInt(int intervalCount, int averageCount, Func<int> randomizer)
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
        private void TestPredictability(int seed, int runLength, Func<int, int, int> successor)
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
    }
}
