using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace AW2.Helpers
{
    /// <summary>
    /// Contains utility methods for generating random numbers.
    /// </summary>
    public class RandomHelper
    {
        #region Fields

        /// <summary>
        /// Global random generator
        /// </summary>
        static Random globalRandomGenerator = new Random(unchecked((int)(DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond)));

        static int[] mixers;

        #endregion

        static RandomHelper()
        {
            mixers = new int[256];
            for (int i = 0; i < mixers.Length; ++i)
                mixers[i] = GetRandomInt();
        }

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
            int mixerIndex = (mixer & 0xff) ^ ((mixer >> 8) & 0xff)
                ^ ((mixer >> 16) & 0xff) ^ ((mixer >> 24) & 0xff);
            return seed ^ mixers[mixerIndex];
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
        /// Finds from a circle sector a random point with an even distribution over the sector's area.
        /// </summary>
        /// <param name="radius">Radius of the circle.</param>
        /// <param name="minAngle">Minimum angle, in radians, for <paramref name="dirAngle"/>.</param>
        /// <param name="maxAngle">Maximum angle, in radians, for <paramref name="dirAngle"/>.</param>
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
            float distance = radius * (float)Math.Sqrt(globalRandomGenerator.NextDouble());
            position = distance * dirUnit;
        }

        #endregion
    }
} 
