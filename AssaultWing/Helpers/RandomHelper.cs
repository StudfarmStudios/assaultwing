// marked in as DEBUG because we don't want NUnit framework to release builds
#if DEBUG
using NUnit.Framework;
#endif
#region Using directives
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

#endregion

namespace AW2.Helpers
{
    /// <summary>
    /// Random helper
    /// </summary>
    public class RandomHelper
    {
        #region Fields

        /// <summary>
        /// Global random generator
        /// </summary>
        static Random globalRandomGenerator = new Random(unchecked((int)(DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond)));

        #endregion

        #region Public interface

        /// <summary>
        /// Get a random int that is at least zero and strictly less than a value.
        /// </summary>
        /// <param name="max">The random int will be strictly less than this value.</param>
        /// <returns>A random int.</returns>
        public static int GetRandomInt(int max)
        {
            return globalRandomGenerator.Next(max);
        }

        /// <summary>
        /// Returns a random int between <see cref="int.MinValue"/> and
        /// <see cref="int.MaxValue"/>.
        /// </summary>
        /// <returns>A random int.</returns>
        public static int GetRandomInt()
        {
            if (globalRandomGenerator.Next(2) == 0)
                return globalRandomGenerator.Next();
            return -1 - globalRandomGenerator.Next();
        }

        /// <summary>
        /// Returns the n'th integer from a random sequence determined by a seed value.
        /// </summary>
        /// This method iterates over n, so use only small values for n.
        /// <param name="seed">Random seed</param>
        /// <param name="n">How manieth value to return from the sequence determined by the seed.</param>
        /// <returns>The n'th integer from the random sequence determined by the seed value.</returns>
        public static int ShiftRandomInt(int seed, int n)
        {
            // The implementation is a linear feedback shift register (with maximal period)
            // which we iterate n times.
            int value = seed;
            for (; n > 0; --n)
                value = ShiftRandomInt(value);
            return value;
        }

        /// <summary>
        /// Mixes a seed value with a mixing value, producing a predictable random value.
        /// </summary>
        /// <param name="seed">Random seed</param>
        /// <param name="mixer">Mixing value</param>
        /// <returns>The "random" integer computed by mixing the seed with the mixer.</returns>
        public static int MixRandomInt(int seed, int mixer)
        {
            // Produce a predictable random number from the mixer and
            // use it to modify seed.
            return seed ^ ShiftRandomInt(mixer);
        }

        /// <summary>
        /// Returns the next integer from a random sequence determined by a seed value.
        /// </summary>
        /// <param name="seed">Random seed</param>
        /// <returns>The next integer from the random sequence determined by the seed value.</returns>
        public static int ShiftRandomInt(int seed)
        {
            // The implementation is a Galois linear feedback shift register (with maximal period).
            // Code adapted from the Wikipedia page
            // http://en.wikipedia.org/wiki/Linear_feedback_shift_register

            // Zero would always yield zero, which is not what one would call random,
            // so we cheat a bit and make zero something else.
            if (seed == 0) seed = 0x73a71143;

            unchecked
            {
                /* taps: 32 31 29 1; characteristic polynomial: x^32 + x^31 + x^29 + x + 1 */
                uint uSeed = (uint)seed;
                uint uNewSeed = (uSeed >> 1) ^ ((uint)(-(uSeed & 1)) & 0xd0000001);
                return (int)uNewSeed;
            }
        }

        /// <summary>
        /// Get random float between min and max
        /// </summary>
        /// <param name="min">Min</param>
        /// <param name="max">Max</param>
        /// <returns>Float</returns>
        public static float GetRandomFloat(float min, float max)
        {
            return (float)globalRandomGenerator.NextDouble() * (max - min) + min;
        }

        /// <summary>
        /// Get random float between 0 and 1
        /// </summary>
        /// <returns>Float</returns>
        public static float GetRandomFloat()
        {
            return (float)globalRandomGenerator.NextDouble();
        }

        /// <summary>
        /// Get random byte between min and max
        /// </summary>
        /// <param name="min">Min</param>
        /// <param name="max">Max</param>
        /// <returns>Byte</returns>
        public static byte GetRandomByte(byte min, byte max)
        {
            return (byte)(globalRandomGenerator.Next(min, max));
        }

        /// <summary>
        /// Get random Vector2
        /// </summary>
        /// <param name="min">Minimum for each component</param>
        /// <param name="max">Maximum for each component</param>
        /// <returns>Vector2</returns>
        public static Vector2 GetRandomVector2(float min, float max)
        {
            return new Vector2(
                GetRandomFloat(min, max),
                GetRandomFloat(min, max));
        }

        /// <summary>
        /// Returns a random Vector2 in a rectangular area.
        /// </summary>
        /// <param name="min">Componentwise minimum.</param>
        /// <param name="max">Componentwise maximum.</param>
        /// <returns>A random Vector2 in a rectangular area.</returns>
        public static Vector2 GetRandomVector2(Vector2 min, Vector2 max)
        {
            return new Vector2(
                GetRandomFloat(min.X, max.X),
                GetRandomFloat(min.Y, max.Y));
        }

        /// <summary>
        /// Get random Vector3
        /// </summary>
        /// <param name="min">Minimum for each component</param>
        /// <param name="max">Maximum for each component</param>
        /// <returns>Vector3</returns>
        public static Vector3 GetRandomVector3(float min, float max)
        {
            return new Vector3(
                GetRandomFloat(min, max),
                GetRandomFloat(min, max),
                GetRandomFloat(min, max));
        }

        /// <summary>
        /// Get random color
        /// </summary>
        /// <returns>Color</returns>
        public static Color GetRandomColor()
        {
            return new Color(new Vector3(
                GetRandomFloat(0.25f, 1.0f),
                GetRandomFloat(0.25f, 1.0f),
                GetRandomFloat(0.25f, 1.0f)));
        }

        /// <summary>
        /// Finds from a circle a random point with an even distribution over the circle's area.
        /// </summary>
        /// <param name="radius">Radius of the circle.</param>
        /// <param name="position">Random point relative to the circle's center.</param>
        /// <param name="dirUnit">Unit vector pointing from the circle's center to the random point.</param>
        /// <param name="dirAngle">Angle, in radians, of <c>dirUnit</c>.</param>
        public static void GetRandomCirclePoint(float radius, 
            out Vector2 position, out Vector2 dirUnit, out float dirAngle)
        {
            GetRandomCirclePoint(radius, 0, MathHelper.TwoPi, out position, out dirUnit, out dirAngle);
        }

        /// <summary>
        /// Finds from a circle a random point with an even distribution over the circle's area.
        /// </summary>
        /// <param name="radius">Radius of the circle.</param>
        /// <param name="minAngle">Minimum angle, in radians, for <paramref name="dirAngle"/>.</param>
        /// <param name="maxAngle">Maximum angle, in radians, for <paramref name="dirAngle"/>.</param>
        /// <param name="position">Random point relative to the circle's center.</param>
        /// <param name="dirUnit">Unit vector pointing from the circle's center to the random point.</param>
        /// <param name="dirAngle">Angle, in radians, of <c>dirUnit</c>.</param>
        public static void GetRandomCirclePoint(float radius, float minAngle, float maxAngle,
            out Vector2 position, out Vector2 dirUnit, out float dirAngle)
        {
            dirAngle = GetRandomFloat(minAngle, maxAngle);
            dirUnit = new Vector2((float)Math.Cos(dirAngle), (float)Math.Sin(dirAngle));
            float distance = radius * (float)Math.Sqrt(globalRandomGenerator.NextDouble());
            position = distance * dirUnit;
        }

        #endregion

        #region Unit tests
#if DEBUG
        /// <summary>
        /// RandomHelper test class.
        /// </summary>
        [TestFixture]
        public class RandomHelperTest
        {
            /// <summary>
            /// Sets up tests.
            /// </summary>
            [SetUp]
            public void SetUp()
            {
            }

            /// <summary>
            /// Tests random ints.
            /// </summary>
            [Test]
            public void TestGetRandomInt()
            {
                TestEvenDistributionInt(65536, 1000, () => GetRandomInt());
            }

            /// <summary>
            /// Tests random int shifting.
            /// </summary>
            [Test]
            public void TestShiftRandomInt()
            {
                int seed = GetRandomInt();
                TestEvenDistributionInt(65536, 1000, () => seed = ShiftRandomInt(seed));
                TestPredictability(seed, 1000, (x, n) => ShiftRandomInt(x));
            }

            /// <summary>
            /// Tests shifting of random int a number of times.
            /// </summary>
            [Test]
            public void TestShiftRandomIntN()
            {
                int seed = GetRandomInt();
                int n = 0;
                TestEvenDistributionInt(64, 300, () => ShiftRandomInt(seed, n++));
                TestPredictability(seed, 1000, (x, k) => ShiftRandomInt(seed, k));
            }

            /// <summary>
            /// Tests mixing of random int.
            /// </summary>
            [Test]
            public void TestMixRandomInt()
            {
                int seed = GetRandomInt();
                int n = 0;
                TestEvenDistributionInt(65536, 1000, () => MixRandomInt(seed, n++));
                TestPredictability(seed, 1000, (x, k) => MixRandomInt(seed, k));
            }

#if POISSON_DISTRIBUTION_IMPLEMENTED
            /// <summary>
            /// Tests random ints with Poisson distribution.
            /// </summary>
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
                int[] counts = new int[intervalCount]; // counts of random numbers grouped into intervals
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
                    + string.Concat( Array.ConvertAll<int, string>(showIntervals, (i) => "\n  " + i + ": " + counts[i]))
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
                int[] run1 = new int[runLength];
                int[] run2 = new int[runLength];
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
#endif
        #endregion Unit tests
    } // class RandomHelper
} 
