// marked in as DEBUG because we don't want NUnit framework to release builds
#if DEBUG
using NUnit.Framework;
#endif
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;

namespace AW2.Helpers
{
    /// <summary>
    /// Provides mathematical helper functions.
    /// </summary>
    class AWMathHelper
    {
        /// <summary>
        /// Linearly interpolates between two values by stepping at most a constant
        /// amount from one towards another.
        /// </summary>
        /// <param name="from">The start value.</param>
        /// <param name="to">The goal value.</param>
        /// <param name="step">The maximum value step.</param>
        /// <returns>The interpolated value.</returns>
        public static float InterpolateTowards(float from, float to, float step)
        {
            return from < to
                ? Math.Min(from + step, to)
                : Math.Max(from - step, to);
        }

        /// <summary>
        /// Linearly interpolates between two angles by stepping at most a constant
        /// amount from one towards another in the direction of shortest distance.
        /// All angles are in radians and are treated modulo 2*pi.
        /// </summary>
        /// <param name="from">The start angle, in radians.</param>
        /// <param name="to">The goal angle, in radians.</param>
        /// <param name="step">The maximum angle step, in positive radians.</param>
        /// <returns>The interpolated angle, in radians.</returns>
        /// Return value is undefined if <b>step</b> is negative.
        public static float InterpolateTowardsAngle(float from, float to, float step)
        {
            // Normalise angles to the interval [0, 2*pi[.
            from = ((from % MathHelper.TwoPi) + MathHelper.TwoPi) % MathHelper.TwoPi;
            to = ((to % MathHelper.TwoPi) + MathHelper.TwoPi) % MathHelper.TwoPi;
            float difference = (((to - from) % MathHelper.TwoPi) + MathHelper.TwoPi) % MathHelper.TwoPi;

            return difference <= MathHelper.Pi
                ? from + Math.Min(step, difference)
                : from - Math.Min(step, MathHelper.TwoPi - difference);
        }

        /// <summary>
        /// Returns the minimum and maximum X and Y coordinates out of
        /// given three tuples of X, Y and Z coordinates.
        /// </summary>
        public static void MinAndMax(Vector3 v1, Vector3 v2, Vector3 v3, out Vector2 min, out Vector2 max)
        {
            min = new Vector2(Math.Min(v1.X, Math.Min(v2.X, v3.X)),
                              Math.Min(v1.Y, Math.Min(v2.Y, v3.Y)));
            max = new Vector2(Math.Max(v1.X, Math.Max(v2.X, v3.X)),
                              Math.Max(v1.Y, Math.Max(v2.Y, v3.Y)));
        }
        
        /// <summary>
        /// Returns the least power of two that is more than or equal to a value.
        /// Return value is undefined for zero and negative arguments.
        /// </summary>
        public static int CeilingPowerTwo(int value)
        {
            // Idea by I0 from http://www.perlmonks.org/?node_id=46889 on 2008-04-23.
            value -= 1;

            // Paint the highest one bit down all the way.
            value |= value >> 1;
            value |= value >> 2;
            value |= value >> 4;
            value |= value >> 8;
            value |= value >> 16;

            return value + 1;
        }

        /// <summary>
        /// Returns the greatest power of two that is less than or equal to a value.
        /// Return value is undefined for zero and negative arguments.
        /// </summary>
        public static int FloorPowerTwo(int value)
        {
            // Paint the highest one bit down all the way.
            value |= value >> 1;
            value |= value >> 2;
            value |= value >> 4;
            value |= value >> 8;
            value |= value >> 16;

            return (value >> 1) + 1;
        }

        #region Unit tests
#if DEBUG
        [TestFixture]
        public class AWMathHelperTest
        {
            [SetUp]
            public void SetUp()
            {
            }

            /// <summary>
            /// Tests interpolating angles.
            /// </summary>
            [Test]
            public void TestAngleInterpolation()
            {
                // Things to consider:
                // A) 'from' is less/more than 'to'
                // B) 'from' and 'to' are in different intervals [n*2*pi, (n+1)*2*pi[
                // C) shortest path is in positive/negative direction
                // D) shortest path crosses a multiple of 2*pi
                // E) 'step' is positive/negative
                // F) shortest distance is more/less than 'step'
                float result;

                // Trivial.
                result = InterpolateTowardsAngle(0, 0, 0);
                Assert.That(AngleEquals(result, 0));

                // Trivial over 2*pi.
                result = InterpolateTowardsAngle(MathHelper.TwoPi, MathHelper.TwoPi, MathHelper.TwoPi);
                Assert.That(AngleEquals(result, 0));

                // A) less; B) no; C) positive; D) no; E) positive; F) more
                result = InterpolateTowardsAngle(0, MathHelper.Pi - 0.001f, MathHelper.PiOver2);
                Assert.That(AngleEquals(result, MathHelper.PiOver2));

                // A) less; B) no; C) positive; D) no; E) positive; F) less
                result = InterpolateTowardsAngle(0, MathHelper.PiOver4, MathHelper.PiOver2);
                Assert.That(AngleEquals(result, MathHelper.PiOver4));

                // A) more; B) no; C) negative; D) no; E) positive; F) more
                result = InterpolateTowardsAngle(MathHelper.Pi, 0.001f, MathHelper.PiOver2);
                Assert.That(AngleEquals(result, MathHelper.PiOver2));

                // A) more; B) no; C) negative; D) no; E) positive; F) less
                result = InterpolateTowardsAngle(MathHelper.PiOver4, 0, MathHelper.PiOver2);
                Assert.That(AngleEquals(result, 0));

                // A) more; B) yes; C) negative; D) yes; E) positive; F) more
                result = InterpolateTowardsAngle(0, -3 * MathHelper.PiOver4, MathHelper.PiOver2);
                Assert.That(AngleEquals(result, -MathHelper.PiOver2));

                // A) more; B) yes; C) negative; D) yes; E) positive; F) less
                result = InterpolateTowardsAngle(0, -3 * MathHelper.PiOver4, MathHelper.Pi);
                Assert.That(AngleEquals(result, -3 * MathHelper.PiOver4));

                // A) less; B) no; C) negative; D) yes; E) positive; F) more
                result = InterpolateTowardsAngle(0, 5 * MathHelper.PiOver4, MathHelper.PiOver2);
                Assert.That(AngleEquals(result, -MathHelper.PiOver2));

                // A) less; B) no; C) negative; D) yes; E) positive; F) less
                result = InterpolateTowardsAngle(0, 5 * MathHelper.PiOver4, MathHelper.Pi);
                Assert.That(AngleEquals(result, -3 * MathHelper.PiOver4));
            }

            /// <summary>
            /// Returns true iff the two angles are equal to sufficient precision.
            /// </summary>
            /// <param name="a">An angle in radians.</param>
            /// <param name="b">Another angle in radians.</param>
            /// <returns>True iff the two angles are equal to sufficient precision.</returns>
            private bool AngleEquals(float a, float b)
            {
                float epsilon = 0.00001f;
                a = ((a % MathHelper.TwoPi) + MathHelper.TwoPi) % MathHelper.TwoPi;
                b = ((b % MathHelper.TwoPi) + MathHelper.TwoPi) % MathHelper.TwoPi;
                return Math.Abs(a - b) < epsilon;
            }

            /// <summary>
            /// Tests obtaining min and max Vector2's from Vector3's.
            /// </summary>
            [Test]
            public void TestMinAndMax()
            {
                Vector2 min, max;
                Vector3 v1 = new Vector3(0, 0, 0);
                Vector3 v2 = new Vector3(1, 2, 3);
                Vector3 v3 = new Vector3(2, 3, 4);
                Vector3 v4 = new Vector3(-10, 10, 100);
                Vector3 v5 = new Vector3(-20, 0, -100);
                Vector3 v6 = new Vector3(-5, -5, -5);

                MinAndMax(v1, v2, v3, out min, out max);
                Assert.AreEqual(new Vector2(0, 0), min);
                Assert.AreEqual(new Vector2(2, 3), max);

                MinAndMax(v3, v1, v2, out min, out max);
                Assert.AreEqual(new Vector2(0, 0), min);
                Assert.AreEqual(new Vector2(2, 3), max);

                MinAndMax(v3, v2, v1, out min, out max);
                Assert.AreEqual(new Vector2(0, 0), min);
                Assert.AreEqual(new Vector2(2, 3), max);

                MinAndMax(v4, v5, v6, out min, out max);
                Assert.AreEqual(new Vector2(-20, -5), min);
                Assert.AreEqual(new Vector2(-5, 10), max);
            }

            /// <summary>
            /// Tests rounding to powers of two.
            /// </summary>
            [Test]
            public void TestRoundPowerTwo()
            {
                for (int power = 0; power < 31; ++power)
                {
                    int n = 1 << power;
                    Assert.AreEqual(n, CeilingPowerTwo(n));
                    Assert.AreEqual(n, FloorPowerTwo(n));
                }
                Assert.AreEqual(4, CeilingPowerTwo(3));
                Assert.AreEqual(2, FloorPowerTwo(3));
                Assert.AreEqual(8, CeilingPowerTwo(7));
                Assert.AreEqual(4, FloorPowerTwo(7));
                Assert.AreEqual(16, CeilingPowerTwo(9));
                Assert.AreEqual(8, FloorPowerTwo(9));
                Assert.AreEqual(0x40000000, CeilingPowerTwo(0x3fffffff));
                Assert.AreEqual(0x20000000, FloorPowerTwo(0x3fffffff));
                Assert.AreEqual(0x40000000, FloorPowerTwo(0x50fa7e57));
            }
        }
#endif
        #endregion Unit tests
    }
}
