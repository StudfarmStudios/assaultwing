using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace AW2.Helpers
{
    /// <summary>
    /// Contains utility methods for generating random numbers.
    /// </summary>
    public static class RandomHelper
    {
        private static Random g_globalRandomGenerator = new Random(unchecked((int)(DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond)));
        private static int[] g_mixers;

        static RandomHelper()
        {
            g_mixers = new int[256];
            for (int i = 0; i < g_mixers.Length; ++i)
                g_mixers[i] = GetRandomInt();
        }

        
        /// <summary>
        /// Get a random int that is at least zero and strictly less than a value.
        /// </summary>
        public static int GetRandomInt(int max)
        {
            return g_globalRandomGenerator.Next(max);
        }

        /// <summary>
        /// Returns randomly -1 or +1 with even distribution.
        /// </summary>
        public static int GetRandomSign()
        {
            return GetRandomInt(2) * 2 - 1;
        }

        /// <summary>
        /// Returns a random int between <see cref="int.MinValue"/> and <see cref="int.MaxValue"/>.
        /// </summary>
        public static int GetRandomInt()
        {
            if (g_globalRandomGenerator.Next(2) == 0)
                return g_globalRandomGenerator.Next();
            return -1 - g_globalRandomGenerator.Next();
        }

        /// <summary>
        /// Mixes a seed value with a mixing value, producing a predictable random value.
        /// </summary>
        public static int MixRandomInt(int seed, int mixer)
        {
            // Produce a predictable random number from the mixer and
            // use it to modify seed.
            int mixerIndex = (mixer & 0xff) ^ ((mixer >> 8) & 0xff)
                ^ ((mixer >> 16) & 0xff) ^ ((mixer >> 24) & 0xff);
            return seed ^ g_mixers[mixerIndex];
        }

        /// <summary>
        /// Returns the next integer from a random sequence determined by a seed value.
        /// </summary>
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
        public static float GetRandomFloat(float min, float max)
        {
            return (float)g_globalRandomGenerator.NextDouble() * (max - min) + min;
        }

        /// <summary>
        /// Get random float between 0 and 1
        /// </summary>
        public static float GetRandomFloat()
        {
            return (float)g_globalRandomGenerator.NextDouble();
        }

        /// <summary>
        /// Get random byte between min and max
        /// </summary>
        public static byte GetRandomByte(byte min, byte max)
        {
            return (byte)(g_globalRandomGenerator.Next(min, max));
        }

        /// <summary>
        /// Get random Vector2
        /// </summary>
        public static Vector2 GetRandomVector2(float min, float max)
        {
            return new Vector2(
                GetRandomFloat(min, max),
                GetRandomFloat(min, max));
        }

        /// <summary>
        /// Returns a random Vector2 in a rectangular area.
        /// </summary>
        public static Vector2 GetRandomVector2(Vector2 min, Vector2 max)
        {
            return new Vector2(
                GetRandomFloat(min.X, max.X),
                GetRandomFloat(min.Y, max.Y));
        }

        /// <summary>
        /// Finds from a circle sector a random point with an even distribution over the sector's area.
        /// </summary>
        public static Vector2 GetRandomCirclePoint(float radius, float minAngle, float maxAngle)
        {
            Vector2 position, dirUnit;
            float dirAngle;
            GetRandomCirclePoint(radius, minAngle, maxAngle, out position, out dirUnit, out dirAngle);
            return position;
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
        /// Finds from a circle sector a random point with an even distribution over the sector's area.
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
            float distance = radius * (float)Math.Sqrt(g_globalRandomGenerator.NextDouble());
            position = distance * dirUnit;
        }

        /// <summary>
        /// Returns a copy of the sequence that is shuffled randomly.
        /// </summary>
        /// <seealso cref="http://en.wikipedia.org/wiki/Fisher-Yates_shuffle#The_modern_algorithm"/>
        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> sequence)
        {
            var result = sequence.ToArray();
            for (int i = result.Length - 1; i >= 1; i--)
            {
                var j = RandomHelper.GetRandomInt(i + 1);
                var swap = result[i];
                result[i] = result[j];
                result[j] = swap;
            }
            return result;
        }
    }
} 
