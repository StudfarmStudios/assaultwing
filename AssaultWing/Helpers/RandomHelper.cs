
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
    } // class RandomHelper
} 
