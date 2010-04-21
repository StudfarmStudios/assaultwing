#if !DEBUG
#define OPTIMIZED_CODE // replace some function calls with fast elementary operations
#endif
#if DEBUG
using NUnit.Framework;
#endif
using System;
using Microsoft.Xna.Framework;

namespace AW2.Helpers
{
    /// <summary>
    /// Provides mathematical helper functions.
    /// </summary>
    public static class AWMathHelper
    {
        /// <summary>
        /// Linearly interpolates between two values by stepping at most a constant
        /// amount from one towards another.
        /// </summary>
        /// <param name="from">The start value.</param>
        /// <param name="to">The goal value.</param>
        /// <param name="step">The maximum value step, a positive value.</param>
        /// <returns>The interpolated value.</returns>
        public static float InterpolateTowards(float from, float to, float step)
        {
            return from < to
                ? Math.Min(from + step, to)
                : Math.Max(from - step, to);
        }

        /// <summary>
        /// Linearly interpolates between two values by stepping at most a constant
        /// distance from one towards another.
        /// </summary>
        /// <param name="step">The maximum value step, a positive value.</param>
        public static Vector2 InterpolateTowards(Vector2 from, Vector2 to, float step)
        {
            var difference = to - from;
            float distanceSquared = difference.LengthSquared();
            return distanceSquared <= step * step
                ? to
                : from + difference / (float)Math.Sqrt(distanceSquared) * step;
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

        /// <summary>
        /// Returns the floor of the binary logarithm of a positive integer,
        /// i.e. the position of the most significant 1 bit. The return value
        /// is undefined for zero and negative values.
        /// </summary>
        /// <param name="value">A positive value.</param>
        /// <returns>The binary logarithm of the value, or undefined for
        /// non-positive values.</returns>
        public static int LogTwo(int value)
        {
            // Algorithm from Warren Jr., Henry S. (2002), Hacker's Delight,
            // Addison Wesley, pp. pp. 215, ISBN 978-0201914658,
            // as presented on Wikipedia on 2008-06-09.
            int pos = 0;
            if (value >= 1 << 16) { value >>= 16; pos += 16; }
            if (value >= 1 << 8) { value >>= 8; pos += 8; }
            if (value >= 1 << 4) { value >>= 4; pos += 4; }
            if (value >= 1 << 2) { value >>= 2; pos += 2; }
            if (value >= 1 << 1) { pos += 1; }
            return pos;
        }

        /// <summary>
        /// Calls <paramref name="plot"/> once for each integer point in a filled circle.
        /// </summary>
        /// <param name="x0">Center X coordinate of the circle.</param>
        /// <param name="y0">Center Y coordinate of the circle.</param>
        /// <param name="radius">Radius of the circle</param>
        /// <param name="plot">The plot method to be called at each circle point.</param>
        public static void FillCircle(int x0, int y0, int radius, Action<int, int> plot)
        {
            // Midpoint circle algorithm, a.k.a. Bresenham's circle algorithm.
            // Implementation adapted from code in Wikipedia, 
            // http://en.wikipedia.org/wiki/Midpoint_circle_algorithm
            // on 2008-06-16.
            if (radius < 0) return;
            int f = 1 - radius;
            int ddF_x = 0;
            int ddF_y = -2 * radius;
            int x = 0;
            int y = radius;

            // Plot the horizontal diameter.
            for (int i = x0 - radius; i <= x0 + radius; ++i)
                plot(i, y0);

            while (x < y)
            {
                if (f >= 0)
                {
                    // Plot horizontal rows starting from top and bottom and
                    // proceeding symmetrically towards the horizontal diameter
                    // on each successive entry to this code block.
                    for (int i = x0 - x; i <= x0 + x; ++i)
                    {
                        plot(i, y0 - y);
                        plot(i, y0 + y);
                    }
                    y--;
                    ddF_y += 2;
                    f += ddF_y;
                }
                x++;
                if (x > y) break;
                ddF_x += 2;
                f += ddF_x + 1;

                // Plot horizontal rows starting immediately above and below
                // the horizontal diameter and proceeding symmetrically towards
                // the top and bottom of the circle.
                for (int i = x0 - y; i <= x0 + y; ++i)
                {
                    plot(i, y0 - x);
                    plot(i, y0 + x);
                }
            }
        }

        /// <summary>
        /// Rounds the components of a vector to the nearest integers.
        /// </summary>
        /// <param name="v">The vector.</param>
        /// <returns>The vector with its components rounded to the nearest integers.</returns>
        public static Vector2 Round(this Vector2 v)
        {
            return new Vector2((float)Math.Round(v.X), (float)Math.Round(v.Y));
        }

        /// <summary>
        /// Returns the smallest non-negative value congruent to a value.
        /// The returned value will be between <c>0</c> and <c>modulus - 1</c>, inclusive.
        /// This is different from the expression <c>value % modulus</c> 
        /// which returns the remainder of the corresponding division.
        /// </summary>
        /// <param name="value">The value to modulate.</param>
        /// <param name="modulus">The modulus.</param>
        /// <returns>The smallest non-negative value congruent to 
        /// <paramref name="value"/> modulo <paramref name="modulus"/>.</returns>
        public static int Modulo(this int value, int modulus)
        {
            if (modulus <= 0) throw new InvalidOperationException("Cannot compute " + value + " modulo " + modulus);
            int result = value % modulus;
            if (result < 0) result += modulus;
            return result;
        }

        /// <summary>
        /// Returns the smallest non-negative value congruent to a value.
        /// The returned value will be between <c>0</c> and <c>modulus</c>, last point excluded.
        /// This is different from the expression <c>value % modulus</c> 
        /// which returns the remainder of the corresponding division.
        /// </summary>
        /// <param name="value">The value to modulate.</param>
        /// <param name="modulus">The modulus.</param>
        /// <returns>The smallest non-negative value congruent to 
        /// <paramref name="value"/> modulo <paramref name="modulus"/>.</returns>
        public static float Modulo(this float value, float modulus)
        {
            if (modulus <= 0) throw new InvalidOperationException("Cannot compute " + value + " modulo " + modulus);
            float result = value % modulus;
            if (result < 0) result += modulus;
            return result;
        }

        /// <summary>
        /// Clamps an integer to an interval.
        /// </summary>
        /// <param name="value">The value to clamp.</param>
        /// <param name="min">The smallest allowed integer.</param>
        /// <param name="max">The largest allowed integer.</param>
        /// <returns>The value clamped to the specified interval.</returns>
        public static int Clamp(this int value, int min, int max)
        {
            if (min > max) throw new ArgumentException("Invalid interval, " + min + " > " + max);
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        /// <summary>
        /// Clamps the length of a Vector2 to an interval.
        /// </summary>
        /// <param name="value">The value to clamp.</param>
        /// <param name="min">The smallest allowed length.</param>
        /// <param name="max">The largest allowed length.</param>
        /// <returns>The value clamped by its length to the specified interval.</returns>
        public static Vector2 Clamp(this Vector2 value, float min, float max)
        {
            if (max < min) throw new ArgumentException("Clamp max smaller than min");
            float length = value.Length();
            float newLength = MathHelper.Clamp(length, min, max);
            if (newLength != length)
            {
                if (value == Vector2.Zero) throw new InvalidOperationException("Cannot scale zero vector");
                if (newLength < 0) throw new InvalidOperationException("Cannot scale vector to negative length");
                return value * newLength / length;
            }
            return value;
        }

        /// <summary>
        /// Returns the number of seconds this <see cref="TimeSpan"/> 
        /// is in the past relative to the current game time.
        /// </summary>
        public static float SecondsAgoGameTime(this TimeSpan time1)
        {
            return (float)(AssaultWing.Instance.GameTime.TotalArenaTime - time1).TotalSeconds;
        }

        /// <summary>
        /// Returns the number of seconds this <see cref="TimeSpan"/> 
        /// is in the past relative to elapsed real time.
        /// </summary>
        public static float SecondsAgoRealTime(this TimeSpan time1)
        {
            return (float)(AssaultWing.Instance.GameTime.TotalRealTime - time1).TotalSeconds;
        }

        /// <summary>
        /// Returns a world matrix given a scaling factor, rotation angle in radians 
        /// and center X and Y coordinates.
        /// </summary>
        public static Matrix CreateWorldMatrix(float scale, float rotation, Vector2 pos)
        {
#if OPTIMIZED_CODE
            float scaledCos = scale * (float)Math.Cos(rotation);
            float scaledSin = scale * (float)Math.Sin(rotation);
            return new Matrix(
                scaledCos, scaledSin, 0, 0,
                -scaledSin, scaledCos, 0, 0,
                0, 0, scale, 0,
                pos.X, pos.Y, 0, 1);
#else
            return Matrix.CreateScale(scale)
                 * Matrix.CreateRotationZ(rotation)
                 * Matrix.CreateTranslation(new Vector3(pos, 0));
#endif
        }

        /// <summary>
        /// Returns <paramref name="v"/> rotated around the positive Z axis 
        /// <paramref name="radians"/> radians.
        /// </summary>
        public static Vector3 RotateZ(this Vector3 v, float radians)
        {
            var cosRadians = (float)Math.Cos(radians);
            var sinRadians = (float)Math.Sin(radians);
            return new Vector3(
                v.X * cosRadians - v.Y * sinRadians,
                v.Y * cosRadians + v.X * sinRadians,
                v.Z);
        }

        /// <summary>
        /// Divides the <see cref="TimeSpan"/> by an integer.
        /// </summary>
        public static TimeSpan Divide(this TimeSpan time, int divisor)
        {
            return TimeSpan.FromTicks(time.Ticks / divisor);
        }

        /// <summary>
        /// Returns a 2D unit vector pointing towards an angle in radians.
        /// </summary>
        public static Vector2 GetUnitVector2(float radians)
        {
            return new Vector2((float)Math.Cos(radians), (float)Math.Sin(radians));
        }

        /// <summary>
        /// Rotates the vector 90 degrees counter-clockwise. This method is
        /// much more effective than multiplying the vector with an appropriate matrix.
        /// </summary>
        public static Vector2 Rotate90(this Vector2 value)
        {
            return new Vector2(-value.Y, value.X);
        }

        #region Unit tests
#if DEBUG
        /// <summary>
        /// Tests for AWMathHelper.
        /// </summary>
        [TestFixture]
        public class AWMathHelperTest
        {
            [Test]
            public void TestInterpolateTowardsFloat()
            {
                Assert.AreEqual(0, InterpolateTowards(0, 0, 0));
                Assert.AreEqual(0, InterpolateTowards(0, 0, 10));
                Assert.AreEqual(1, InterpolateTowards(0, 1, 10));
                Assert.AreEqual(-1, InterpolateTowards(0, -1, 10));
                Assert.AreEqual(10, InterpolateTowards(0, 11, 10));
                Assert.AreEqual(-10, InterpolateTowards(0, -11, 10));
            }

            [Test]
            public void TestInterpolateTowardsVector2()
            {
                var p1 = Vector2.Zero;
                var p2 = Vector2.UnitX;
                var p3 = Vector2.UnitY;
                var p4 = new Vector2(3, -4);
                Assert.AreEqual(p1, InterpolateTowards(p1, p1, 0));
                Assert.AreEqual(p1, InterpolateTowards(p1, p1, 10));
                Assert.AreEqual(p2, InterpolateTowards(p1, p2, 10));
                Assert.AreEqual(p3, InterpolateTowards(p1, p3, 10));
                Assert.AreEqual(p4, InterpolateTowards(p1, p4, 10));
                Assert.AreEqual(-p4, InterpolateTowards(p1, -p4, 10));
                Assert.AreEqual(10 * Vector2.UnitX, InterpolateTowards(p1, 11 * Vector2.UnitX, 10));
                Assert.AreEqual(-p4, InterpolateTowards(p1, -2 * p4, 5));
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

            /// <summary>
            /// Tests binary logarithm.
            /// </summary>
            [Test]
            public void TestLogTwo()
            {
                Assert.AreEqual(0, LogTwo(1));
                Assert.AreEqual(1, LogTwo(2));
                Assert.AreEqual(1, LogTwo(3));
                Assert.AreEqual(2, LogTwo(4));
                Assert.AreEqual(30, LogTwo(0x7fffffff));
                Assert.AreEqual(30, LogTwo(0x40000001));
                Assert.AreEqual(30, LogTwo(0x40000000));
                Assert.AreEqual(29, LogTwo(0x3fffffff));
                for (int i = 1; i < 0x40000000; i += i < 0x1234 ? 1 : 1301)
                {
                    Assert.AreEqual(CeilingPowerTwo(i + 1), 1 << (1 + LogTwo(i)));
                    Assert.AreEqual(FloorPowerTwo(i), 1 << LogTwo(i));
                }
            }

            /// <summary>
            /// Tests circle filling.
            /// </summary>
            [Test]
            public void TestFillCircle()
            {
                for (int radius = 0; radius < 15; ++radius)
                    DoFillCircleTest(radius);
                DoFillCircleTest(313);
                DoFillCircleTest(314);
                DoFillCircleTest(1001);
            }

            /// <summary>
            /// Tests Vector2 rounding
            /// </summary>
            [Test]
            public void TestVector2Round()
            {
                Assert.AreEqual(new Vector2(0, 0), Round(new Vector2(0, 0)));
                Assert.AreEqual(new Vector2(1, 1), Round(new Vector2(1.000001f, 1.499999f)));
                Assert.AreEqual(new Vector2(2, 2), Round(new Vector2(1.500001f, 1.999999f)));
                Assert.AreEqual(new Vector2(-1, -1), Round(new Vector2(-1.000001f, -1.499999f)));
                Assert.AreEqual(new Vector2(-2, -2), Round(new Vector2(-1.500001f, -1.999999f)));
                Assert.AreEqual(new Vector2(-1234567, 1234567), Round(new Vector2(-1234566.7f, 1234567.3f)));
            }

            /// <summary>
            /// Tests integer congruence.
            /// </summary>
            [Test]
            public void TestModulo()
            {
                try { 0.Modulo(0); Assert.Fail("Exception not thrown with zero modulus"); }
                catch { }
                try { 5.Modulo(-3); Assert.Fail("Exception not thrown with negative modulus"); }
                catch { }

                Assert.AreEqual(1, 1.Modulo(3));
                Assert.AreEqual(2, 2.Modulo(3));
                Assert.AreEqual(0, 3.Modulo(3));
                Assert.AreEqual(1, 4.Modulo(3));

                Assert.AreEqual(1234567, 3234567.Modulo(2000000));

                Assert.AreEqual(2, (-1).Modulo(3));
                Assert.AreEqual(1, (-2).Modulo(3));
                Assert.AreEqual(0, (-3).Modulo(3));
                Assert.AreEqual(2, (-4).Modulo(3));

                Assert.AreEqual(-3234567 + 2 * 2000000, (-3234567).Modulo(2000000));
            }

            /// <summary>
            /// Tests Vector2 clamping
            /// </summary>
            [Test]
            public void TestVector2Clamp()
            {
                Assert.AreEqual(Vector2.Zero, Vector2.Zero.Clamp(0, 0));
                Assert.AreEqual(Vector2.Zero, Vector2.One.Clamp(0, 0));
                Assert.AreEqual(5 * Vector2.UnitX, Vector2.UnitX.Clamp(5, 10));
                Assert.AreEqual(7 * Vector2.UnitY, (9 * Vector2.UnitY).Clamp(3, 7));
                Assert.AreEqual(new Vector2(-3, -4), new Vector2(-6, -8).Clamp(1, 5));
                try
                {
                    Vector2.One.Clamp(-2, -1);
                    Assert.Fail("Failed to throw exception");
                }
                catch { }
                try
                {
                    Vector2.One.Clamp(5, 3);
                    Assert.Fail("Failed to throw exception");
                }
                catch { }
                try
                {
                    Vector2.Zero.Clamp(1, 2);
                    Assert.Fail("Failed to throw exception");
                }
                catch { }
            }

            [Test]
            public void TestRotate90()
            {
                Assert.AreEqual(new Vector2(0, 0), new Vector2(0, 0).Rotate90());
                Assert.AreEqual(new Vector2(0, 1), new Vector2(1, 0).Rotate90());
                Assert.AreEqual(new Vector2(-1, 0), new Vector2(0, 1).Rotate90());
                Assert.AreEqual(new Vector2(0, -1), new Vector2(-1, 0).Rotate90());
                Assert.AreEqual(new Vector2(1, 0), new Vector2(0, -1).Rotate90());
                Assert.AreEqual(new Vector2(-40, 90), new Vector2(90, 40).Rotate90());
                Assert.AreEqual(new Vector2(0.02f, -0.01f), new Vector2(-0.01f, -0.02f).Rotate90());
            }

            /// <summary>
            /// Helper for TestFillCircle()
            /// </summary>
            private void DoFillCircleTest(int radius)
            {
                Console.Out.WriteLine("DoFillCircleTest(" + radius + ")");
                int x0 = radius;
                int y0 = radius;
                int[,] data = new int[2 * radius + 1, 2 * radius + 1]; // indexed as data[y, x]
                FillCircle(x0, y0, radius, delegate(int x, int y)
                {
                    Assert.That(x >= 0 && y >= 0 && x < data.GetLength(1) && y < data.GetLength(0),
                        "FillCircle plotted outside the containing square at (" + x + ", " + y + ")");
                    ++data[y, x];
                });
                
                // Draw data.
                if (radius < 9)
                    for (int y = 0; y < data.GetLength(0); ++y)
                    {
                        for (int x = 0; x < data.GetLength(1); ++x)
                            Console.Out.Write(data[y, x] > 0 ? (char)('0' + data[y, x]) : 'o');
                        Console.Out.WriteLine();
                    }

                // Make sure all data is 0 or 1.
                for (int y = 0; y < data.GetLength(0); ++y)
                    for (int x = 0; x < data.GetLength(1); ++x)
                        Assert.That(data[y, x] <= 1, "FillCircle plotted the same point (" + x + ", " + y + ") " + data[y, x] + " times");
                
                // Make sure plotted area is continuous horizontally and vertically.
                for (int y = 0; y < data.GetLength(0); ++y)
                {
                    int phase = 0; // 0=not started; 1=started; 2=finished
                    for (int x = 0; x < data.GetLength(1); ++x)
                    {
                        if (phase == 0)
                        {
                            if (data[y, x] == 1) ++phase;
                        }
                        else if (phase == 1)
                        {
                            if (data[y, x] == 0) ++phase;
                        }
                        else
                            Assert.That(data[y, x] == 0, "Horizontal line is not continuous at (" + x + "," + y + ")");
                    }
                }
                for (int x = 0; x < data.GetLength(1); ++x)
                {
                    int phase = 0; // 0=not started; 1=started; 2=finished
                    for (int y = 0; y < data.GetLength(0); ++y)
                    {
                        if (phase == 0)
                        {
                            if (data[y, x] == 1) ++phase;
                        }
                        else if (phase == 1)
                        {
                            if (data[y, x] == 0) ++phase;
                        }
                        else
                            Assert.That(data[y, x] == 0, "Vertical line is not continuous at (" + x + "," + y + ")");
                    }
                }

                // Make sure that a diamond shape fits in the filled circle.
                for (int i = 0; i <= radius; ++i)
                {
                    Assert.That(data[y0 + radius - i, x0 + i] == 1, "Diamond shape not contained in circle at (" + (x0 + i) + "," + (y0 + radius - i) + ")");
                    Assert.That(data[y0 + radius - i, x0 - i] == 1, "Diamond shape not contained in circle at (" + (x0 - i) + "," + (y0 + radius - i) + ")");
                    Assert.That(data[y0 - radius + i, x0 + i] == 1, "Diamond shape not contained in circle at (" + (x0 + i) + "," + (y0 - radius + i) + ")");
                    Assert.That(data[y0 - radius + i, x0 - i] == 1, "Diamond shape not contained in circle at (" + (x0 - i) + "," + (y0 - radius + i) + ")");
                }
            }
        }
#endif
        #endregion Unit tests
    }
}
