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
        }
#endif
        #endregion Unit tests
    }
}
