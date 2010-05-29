#if !DEBUG
#define OPTIMIZED_CODE // replace some function calls with fast elementary operations
#endif
#if DEBUG
using NUnit.Framework;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace AW2.Helpers
{
    /// <summary>
    /// Provides mathematical helper functions.
    /// </summary>
    public static class AWMathHelper
    {
        public delegate void PointPlotDelegate(int x, int y);

        private struct Scanline
        {
            private int _y;
            private int _x1;
            private int _x2;

            public int Y { get { return _y; } }

            public Scanline(int y, int x)
            {
                _y = y;
                _x1 = x;
                _x2 = x;
            }

            public void Include(int x)
            {
                if (x < _x1) _x1 = x;
                else if (x > _x2) _x2 = x;
            }

            /// <summary>
            /// Includes X coordinates interpolated from the endpoints of two other scanlines
            /// at this scanline's Y coordinate.
            /// </summary>
            public void IncludeInterpolated(Scanline scan1, Scanline scan3)
            {
                Include((scan3._x1 * (_y - scan1._y) + scan1._x1 * (scan3._y - _y)).DivRound(scan3._y - scan1._y));
                Include((scan3._x2 * (_y - scan1._y) + scan1._x2 * (scan3._y - _y)).DivRound(scan3._y - scan1._y));
            }

            /// <summary>
            /// Calls <paramref name="plot"/> for points in the trapezoid spanned by two scanlines.
            /// The points with maximal X or Y coordinates are left out.
            /// </summary>
            public static void InterpolatePlot(PointPlotDelegate plot, Scanline scan1, Scanline scan2)
            {
                int divisor = scan2._y - scan1._y;
                if (divisor == 0) divisor = 1;
                int x1Step = scan2._x1 - scan1._x1;
                int x2Step = scan2._x2 - scan1._x2;
                for (var scanline = new Scanline { _y = scan1._y, _x1 = scan1._x1 * divisor, _x2 = scan1._x2 * divisor };
                    scanline._y < scan2._y;
                    ++scanline._y, scanline._x1 += x1Step, scanline._x2 += x2Step)
                {
                    int x1 = scanline._x1.DivRound(divisor);
                    int x2 = scanline._x2.DivRound(divisor);
                    for (int x = x1; x < x2; ++x) plot(x, scanline._y);
                }
            }
        }

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
        /// Division rounded to the nearest integer, midpoints rounded towards positive.
        /// </summary>
        public static int DivRound(this int value, int divisor)
        {
            if (divisor == 0) throw new ArgumentException("Divisor must not be zero");
            int sign = (value < 0) ^ (divisor < 0) ? -1 : 1;
            if (value < 0) value = -value;
            if (divisor < 0) divisor = -divisor;
            int rounder = sign > 0 ? divisor : divisor - 1;
            return sign * ((value * 2 + rounder) / (divisor * 2));
        }

        /// <summary>
        /// Calls <paramref name="plot"/> once for each integer point in a filled circle.
        /// </summary>
        /// <param name="x0">Center X coordinate of the circle.</param>
        /// <param name="y0">Center Y coordinate of the circle.</param>
        /// <param name="radius">Radius of the circle</param>
        /// <param name="plot">The plot method to be called at each circle point.</param>
        public static void FillCircle(int x0, int y0, int radius, PointPlotDelegate plot)
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
        /// Calls <paramref name="plot"/> once for each integer point in a filled triangle.
        /// The points of the triangle that are maximal in X or Y coordinate are
        /// not plotted to allow plotting two triangles that share a side without
        /// the points of the two triangles overlapping each other.
        /// </summary>
        public static void FillTriangle(Point point1, Point point2, Point point3, PointPlotDelegate plot)
        {
            // Sort points by increasing Y
            if (point2.Y < point1.Y) Swap(ref point1, ref point2);
            if (point3.Y < point2.Y)
            {
                Swap(ref point2, ref point3);
                if (point2.Y < point1.Y) Swap(ref point1, ref point2);
            }

            // Find master scanlines
            var scan1 = new Scanline(point1.Y, point1.X);
            if (point2.Y == point1.Y) scan1.Include(point2.X);
            if (point3.Y == point1.Y) scan1.Include(point3.X);
            var scan2 = scan1;
            if (point2.Y > scan1.Y)
            {
                scan2 = new Scanline(point2.Y, point2.X);
                if (point3.Y == point2.Y) scan2.Include(point3.X);
            }
            else if (point3.Y > scan1.Y)
            {
                scan2 = new Scanline(point3.Y, point3.X);
            }
            var scan3 = scan2;
            if (point3.Y > scan2.Y)
            {
                scan3 = new Scanline(point3.Y, point3.X);
                scan2.IncludeInterpolated(scan3, scan1);
            }

            // Loop through Y, interpolating between master scanlines
            Scanline.InterpolatePlot(plot, scan1, scan2);
            Scanline.InterpolatePlot(plot, scan2, scan3);
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
        /// Returns the angle of the vector. The X unit vector has angle zero and
        /// the positive winding direction is counter-clockwise.
        /// </summary>
        public static float Angle(this Vector2 v)
        {
            float asin = (float)Math.Asin(v.Y);
            if (v.X < 0) return MathHelper.Pi - asin;
            return asin;
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
            return (float)(AssaultWing.Instance.DataEngine.ArenaTotalTime - time1).TotalSeconds;
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

        private static void Swap(ref Point point1, ref Point point2)
        {
            var swap = point1;
            point1 = point2;
            point2 = swap;
        }

        #region Unit tests
#if DEBUG
        /// <summary>
        /// Tests for AWMathHelper.
        /// </summary>
        [TestFixture]
        public class AWMathHelperTest
        {
            private class PlotData : IEnumerable<int>
            {
                private int[,] _data;

                public int Width { get { return _data.GetLength(1); } }
                public int Height { get { return _data.GetLength(0); } }
                public int this[int y, int x] { get { return _data[y, x]; } }

                public PlotData(int width, int height)
                {
                    _data = new int[height, width];
                }

                public void Plot(int x, int y)
                {
                    Assert.That(x >= 0 && y >= 0 && x < Width && y < Height,
                        "Point plotted outside the containing square at (" + x + ", " + y + ")");
                    ++_data[y, x];
                }

                public void DebugDraw()
                {
                    for (int y = 0; y < Height; ++y)
                    {
                        for (int x = 0; x < Width; ++x)
                            Console.Out.Write(_data[y, x] > 0 ? (char)('0' + _data[y, x]) : 'o');
                        Console.Out.WriteLine();
                    }
                }

                public Point? GetFirstDuplicatePlotPoint()
                {
                    for (int y = 0; y < Height; ++y)
                        for (int x = 0; x < Width; ++x)
                            if (_data[y, x] > 1) return new Point(x, y);
                    return null;
                }

                public IEnumerator<int> GetEnumerator()
                {
                    for (int y = 0; y < Height; ++y)
                        for (int x = 0; x < Width; ++x)
                            yield return _data[y, x];
                }

                System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
                {
                    return GetEnumerator();
                }
            }

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

            private bool AngleEquals(float a, float b)
            {
                float epsilon = 0.00001f;
                a = ((a % MathHelper.TwoPi) + MathHelper.TwoPi) % MathHelper.TwoPi;
                b = ((b % MathHelper.TwoPi) + MathHelper.TwoPi) % MathHelper.TwoPi;
                return Math.Abs(a - b) < epsilon;
            }

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

            [Test]
            public void TestDivRound()
            {
                Assert.AreEqual(0, 0.DivRound(1));
                Assert.AreEqual(1, 1.DivRound(1));
                Assert.AreEqual(1, 2.DivRound(2));
                Assert.AreEqual(2, 3.DivRound(2));
                Assert.AreEqual(2, 4.DivRound(2));

                Assert.AreEqual(2, 6.DivRound(3));
                Assert.AreEqual(2, 7.DivRound(3));
                Assert.AreEqual(3, 8.DivRound(3));
                Assert.AreEqual(3, 9.DivRound(3));
                Assert.AreEqual(2, (-6).DivRound(-3));
                Assert.AreEqual(2, (-7).DivRound(-3));
                Assert.AreEqual(3, (-8).DivRound(-3));
                Assert.AreEqual(3, (-9).DivRound(-3));

                Assert.AreEqual(-2, 6.DivRound(-3));
                Assert.AreEqual(-2, 7.DivRound(-3));
                Assert.AreEqual(-3, 8.DivRound(-3));
                Assert.AreEqual(-3, 9.DivRound(-3));
                Assert.AreEqual(-2, (-6).DivRound(3));
                Assert.AreEqual(-2, (-7).DivRound(3));
                Assert.AreEqual(-3, (-8).DivRound(3));
                Assert.AreEqual(-3, (-9).DivRound(3));

                Assert.AreEqual(3, 12.DivRound(4));
                Assert.AreEqual(3, 13.DivRound(4));
                Assert.AreEqual(4, 14.DivRound(4));
                Assert.AreEqual(4, 15.DivRound(4));
                Assert.AreEqual(4, 16.DivRound(4));
                Assert.AreEqual(3, (-12).DivRound(-4));
                Assert.AreEqual(3, (-13).DivRound(-4));
                Assert.AreEqual(4, (-14).DivRound(-4));
                Assert.AreEqual(4, (-15).DivRound(-4));
                Assert.AreEqual(4, (-16).DivRound(-4));

                Assert.AreEqual(-3, 12.DivRound(-4));
                Assert.AreEqual(-3, 13.DivRound(-4));
                Assert.AreEqual(-3, 14.DivRound(-4));
                Assert.AreEqual(-4, 15.DivRound(-4));
                Assert.AreEqual(-4, 16.DivRound(-4));
                Assert.AreEqual(-3, (-12).DivRound(4));
                Assert.AreEqual(-3, (-13).DivRound(4));
                Assert.AreEqual(-3, (-14).DivRound(4));
                Assert.AreEqual(-4, (-15).DivRound(4));
                Assert.AreEqual(-4, (-16).DivRound(4));
            }

            [Test]
            public void TestFillCircle()
            {
                for (int radius = 0; radius < 15; ++radius)
                    DoFillCircleTest(radius);
                DoFillCircleTest(313);
                DoFillCircleTest(314);
            }

            [Test]
            public void TestFillTriangle()
            {
                DoFillTriangleTest(new Point(0, 0), new Point(5, 9), new Point(3, 2));
                DoFillTriangleTest(new Point(-5, 5), new Point(5, -5), new Point(5, 5));
                DoFillTriangleTest(new Point(5, 5), new Point(-5, 5), new Point(5, -5));
            }

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

            [Test]
            public void TestAngle()
            {
                Assert.AreEqual(0, new Vector2(0, 0).Angle());
                Assert.AreEqual(0, new Vector2(1, 0).Angle());
                Assert.AreEqual(MathHelper.PiOver2, new Vector2(0, 1).Angle());
                Assert.AreEqual(-MathHelper.PiOver2, new Vector2(0, -1).Angle());
                Assert.AreEqual(MathHelper.Pi, new Vector2(-1, 0).Angle());
            }

            [Test]
            public void TestModulo()
            {
                Assert.Throws<InvalidOperationException>(() => 0.Modulo(0), "Exception not thrown with zero modulus");
                Assert.Throws<InvalidOperationException>(() => 5.Modulo(-3), "Exception not thrown with negative modulus");

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

            [Test]
            public void TestVector2Clamp()
            {
                Assert.AreEqual(Vector2.Zero, Vector2.Zero.Clamp(0, 0));
                Assert.AreEqual(Vector2.Zero, Vector2.One.Clamp(0, 0));
                Assert.AreEqual(5 * Vector2.UnitX, Vector2.UnitX.Clamp(5, 10));
                Assert.AreEqual(7 * Vector2.UnitY, (9 * Vector2.UnitY).Clamp(3, 7));
                Assert.AreEqual(new Vector2(-3, -4), new Vector2(-6, -8).Clamp(1, 5));
                Assert.Throws<InvalidOperationException>(() => Vector2.One.Clamp(-2, -1));
                Assert.Throws<ArgumentException>(() => Vector2.One.Clamp(5, 3));
                Assert.Throws<InvalidOperationException>(() => Vector2.Zero.Clamp(1, 2));
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

            private void DoFillTriangleTest(Point point1, Point point2, Point point3)
            {
                Console.Out.WriteLine(string.Format("DoFillTriangleTest({0}, {1}, {2})", point1, point2, point3));
                int xMin = Math.Min(Math.Min(point1.X, point2.X), point3.X);
                int yMin = Math.Min(Math.Min(point1.Y, point2.Y), point3.Y);
                int xMax = Math.Max(Math.Max(point1.X, point2.X), point3.X);
                int yMax = Math.Max(Math.Max(point1.Y, point2.Y), point3.Y);
                var data = new PlotData(xMax - xMin + 1, yMax - yMin + 1);
                FillTriangle(point1, point2, point3, (x, y) => data.Plot(x - xMin, y - yMin));
                data.DebugDraw();
                var duplicate = data.GetFirstDuplicatePlotPoint();
                Assert.That(!duplicate.HasValue, "FillTriangle plotted " + duplicate + " several times");
                Assert.That(data.Any(x => x > 0), "FillTriangle plotted nothing");
            }

            private void DoFillCircleTest(int radius)
            {
                Console.Out.WriteLine("DoFillCircleTest(" + radius + ")");
                int x0 = radius;
                int y0 = radius;
                var data = new PlotData(2 * radius + 1, 2 * radius + 1);
                FillCircle(x0, y0, radius, data.Plot);
                if (radius < 9) data.DebugDraw();
                var duplicate = data.GetFirstDuplicatePlotPoint();
                Assert.That(!duplicate.HasValue, "FillCircle plotted " + duplicate + " several times");
                
                // Make sure plotted area is continuous horizontally and vertically.
                for (int y = 0; y < data.Height; ++y)
                {
                    int phase = 0; // 0=not started; 1=started; 2=finished
                    for (int x = 0; x < data.Width; ++x)
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
                for (int x = 0; x < data.Width; ++x)
                {
                    int phase = 0; // 0=not started; 1=started; 2=finished
                    for (int y = 0; y < data.Height; ++y)
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
