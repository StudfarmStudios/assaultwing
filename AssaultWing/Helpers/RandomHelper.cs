// marked in as DEBUG because we don't want NUnit framework to release builds
#if DEBUG
using NUnit.Framework;
#endif
#region Using directives
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
#endregion

namespace AW2.Helpers
{
    /// <summary>
    /// Random helper
    /// </summary>
    public class RandomHelper
    {
        #region Variables
        /// <summary>
        /// Global random generator
        /// </summary>
        public static Random globalRandomGenerator = GenerateNewRandomGenerator();
        #endregion

        #region Generate a new random generator
        /// <summary>
        /// Generate a new random generator with help of
        /// WindowsHelper.GetPerformanceCounter.
        /// Also used for all GetRandom methods here.
        /// </summary>
        /// <returns>Random</returns>
        public static Random GenerateNewRandomGenerator()
        {
            globalRandomGenerator =
                new Random((int)DateTime.Now.Ticks);
            //needs Interop: (int)WindowsHelper.GetPerformanceCounter());
            return globalRandomGenerator;
        } // GenerateNewRandomGenerator()
        #endregion

        #region Get random float and byte methods
        /// <summary>
        /// Get a random int that is at least zero and strictly less than a value.
        /// </summary>
        /// <param name="max">The random int will be strictly less than this value.</param>
        /// <returns>A random int.</returns>
        public static int GetRandomInt(int max)
        {
            return globalRandomGenerator.Next(max);
        } // GetRandomInt(max)

        /// <summary>
        /// Returns a random int.
        /// </summary>
        /// <returns>A random int.</returns>
        public static int GetRandomInt()
        {
            if (globalRandomGenerator.Next(2) == 0)
                return globalRandomGenerator.Next();
            return -1 - globalRandomGenerator.Next();
        } // GetRandomInt()

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
                ShiftRandomInt(seed);
            return value;
        }

        /// <summary>
        /// Returns the next integer from a random sequence determined by a seed value.
        /// </summary>
        /// <param name="seed">Random seed</param>
        /// <returns>The next integer from the random sequence determined by the seed value.</returns>
        public static int ShiftRandomInt(int seed)
        {
            // The implementation is a linear feedback shift register (with maximal period).
            return (seed >> 1) ^ (-(seed & 1) & -0x50000001);
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
        } // GetRandomFloat(min, max)

        /// <summary>
        /// Get random float between 0 and 1
        /// </summary>
        /// <returns>Float</returns>
        public static float GetRandomFloat()
        {
            return (float)globalRandomGenerator.NextDouble();
        } // GetRandomFloat(min, max)


        /// <summary>
        /// Get random byte between min and max
        /// </summary>
        /// <param name="min">Min</param>
        /// <param name="max">Max</param>
        /// <returns>Byte</returns>
        public static byte GetRandomByte(byte min, byte max)
        {
            return (byte)(globalRandomGenerator.Next(min, max));
        } // GetRandomByte(min, max)

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
        } // GetRandomVector2(min, max)

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
        } // GetRandomVector2(min, max)

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
        } // GetRandomVector3(min, max)

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
        } // GetRandomColor()

        #endregion

        #region Unit tests
#if DEBUG
        /// <summary>
        /// RandomHelper test class.
        /// </summary>
        [TestFixture]
        public class RandomHelperTest
        {
            delegate int IntRandomizer();

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
                TestEvenDistributionInt(delegate() { return GetRandomInt(); });
            }

            /// <summary>
            /// Tests random int shifting.
            /// </summary>
            [Test]
            public void TestRandomShiftInt()
            {
                int seed = GetRandomInt();
                TestEvenDistributionInt(delegate() { return seed = ShiftRandomInt(seed); });
            }

            /// <summary>
            /// Helper method. Tests for even distribution of ints given by an
            /// int randomising delegate.
            /// </summary>
            void TestEvenDistributionInt(IntRandomizer randomizer)
            {
                int[] counts = new int[65536]; // counts for 2^16 intervals, each of length 2^16
                for (int i = 0; i < 65536000; ++i)
                {
                    int value = randomizer();
                    int interval = (int)(((long)value - int.MinValue) / 65536);
                    ++counts[interval];
                }

                // Expect a count of 1000 in each interval.
                int epsilon = 0;
                int worstInterval = -1;
                for (int i = 0; i < 65536; ++i)
                    if (Math.Abs(1000 - counts[i]) > epsilon)
                    {
                        epsilon = Math.Abs(1000 - counts[i]);
                        worstInterval = i;
                    }
                Assert.Less(epsilon, 160, "Random distribution doesn't look very even, worst interval " + worstInterval
                    + "\nsome intervals: 0=" + counts[0] + ", 1=" + counts[1] + ", 2=" + counts[2]
                    + "\n32766=" + counts[32766] + ", 32767=" + counts[32767] + ", 32768=" + counts[32768]
                    + "\n65533=" + counts[65533] + ", 65534=" + counts[65534] + ", 65535=" + counts[65535]
                    + "\n*** Please rerun the test several times and worry only if it fails repeatedly ***");
            }
        }
#endif
        #endregion Unit tests
    } // class RandomHelper
} 
