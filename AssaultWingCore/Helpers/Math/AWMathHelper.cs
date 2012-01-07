#if !DEBUG
#define OPTIMIZED_CODE // replace some function calls with fast elementary operations
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using AW2.Core;

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
            float difference = GetMinimalPositiveEqualAngle(to - from);
            return difference <= MathHelper.Pi
                ? from + Math.Min(step, difference)
                : from - Math.Min(step, MathHelper.TwoPi - difference);
        }

        /// <summary>
        /// Returns the smallest angle that is positive and that denotes
        /// an equal rotation to the given angle.
        /// </summary>
        public static float GetMinimalPositiveEqualAngle(float angle)
        {
            return angle >= 0
                ? angle % MathHelper.TwoPi
                : MathHelper.TwoPi + (angle % MathHelper.TwoPi);
        }

        /// <summary>
        /// Returns the angle that is minimal in its absolute value and that denotes
        /// an equal rotation to the given angle.
        /// </summary>
        public static float GetAbsoluteMinimalEqualAngle(float angle)
        {
            return GetMinimalPositiveEqualAngle(angle + MathHelper.Pi) - MathHelper.Pi;
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
        /// Truncates the floating-point Vector2 into an integral Point.
        /// </summary>
        public static Point ToPoint(this Vector2 v)
        {
            return new Point((int)v.X, (int)v.Y);
        }

        /// <summary>
        /// Returns the angle of the vector between -pi and pi. The X unit vector has angle zero and
        /// the positive winding direction is counter-clockwise.
        /// </summary>
        public static float Angle(this Vector2 v)
        {
            float yDivLength = v.X == 0 ? Math.Sign(v.Y) : v.Y / v.Length();
            float asin = (float)Math.Asin(yDivLength);
            if (v.X >= 0) return asin;
            if (v.Y >= 0) return MathHelper.Pi - asin;
            return -MathHelper.Pi - asin;
        }

        /// <summary>
        /// Returns the absolute difference of two angles in radians
        /// as the smallest non-negative angle congruent to 2 * PI.
        /// </summary>
        public static float AbsoluteAngleDifference(float angle1, float angle2)
        {
            float moduloDifference = (angle1 - angle2).Modulo(MathHelper.TwoPi);
            return moduloDifference <= MathHelper.Pi
                ? moduloDifference
                : MathHelper.TwoPi - moduloDifference;
        }

        /// <summary>
        /// Clamps the angle value between two angle values, inclusive.
        /// </summary>
        public static float ClampAngle(this float angle, float minAngle, float maxAngle)
        {
            if (minAngle > maxAngle) throw new ArgumentException("Minimum angle cannot be greater than maximum angle");
            if (maxAngle > minAngle + MathHelper.TwoPi) throw new ArgumentException("Clamp supports only ranges up to full circle");
            var minAngleNormalized = GetMinimalPositiveEqualAngle(minAngle);
            var angleNormalized = GetMinimalPositiveEqualAngle(angle);
            var equalAngleAboveMinAngle = angleNormalized + minAngle - minAngleNormalized +
                (angleNormalized < minAngleNormalized ? MathHelper.TwoPi : 0);
            if (minAngle <= equalAngleAboveMinAngle && equalAngleAboveMinAngle <= maxAngle) return equalAngleAboveMinAngle;
            var middleInvertRange = (maxAngle + minAngle + MathHelper.TwoPi) / 2;
            return equalAngleAboveMinAngle < middleInvertRange ? maxAngle : minAngle;
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
        /// Clamps width and height to the Rectangle, preserving aspect ratio.
        /// </summary>
        public static void Clamp(this Rectangle rect, ref int width, ref int height)
        {
            if (width == 0 || height == 0) throw new ArgumentException("Width and height must be non-zero");
            if (width <= rect.Width && height <= rect.Height) return;
            float widthScale = rect.Width / (float)width;
            float heightScale = rect.Height / (float)height;
            if (widthScale < heightScale)
            {
                height = height * rect.Width / width;
                width = rect.Width;
            }
            else
            {
                width = width * rect.Height / height;
                height = rect.Height;
            }
        }

        /// <summary>
        /// Stretches width and height to the Rectangle, preserving aspect ratio.
        /// </summary>
        public static void Stretch(this Rectangle rect, ref int width, ref int height)
        {
            if (width == 0 || height == 0) throw new ArgumentException("Width and height must be non-zero");
            if (width * rect.Height < rect.Width * height)
            {
                // Limited by height
                width = rect.Height * width / height;
                height = rect.Height;
            }
            else
            {
                // Limited by width
                height = rect.Width * height / width;
                width = rect.Width;
            }
        }

        /// <summary>
        /// Returns a Rectangle that is obtained by moving <paramref name="rect"/> as little
        /// as possible to contain the <paramref name="p"/>.
        /// </summary>
        public static Rectangle MoveToContain(this Rectangle rect, Point p)
        {
            rect.X = Math.Min(rect.X, p.X);
            rect.Y = Math.Min(rect.Y, p.Y);
            rect.X = Math.Max(rect.X + rect.Width - 1, p.X) - (rect.Width - 1);
            rect.Y = Math.Max(rect.Y + rect.Height - 1, p.Y) - (rect.Height - 1);
            return rect;
        }

        /// <summary>
        /// Returns the number of seconds this <see cref="TimeSpan"/> 
        /// is in the past relative to the current game time.
        /// </summary>
        public static float SecondsAgoGameTime(this TimeSpan time1)
        {
            return (float)(AssaultWingCore.Instance.DataEngine.ArenaTotalTime - time1).TotalSeconds;
        }

        /// <summary>
        /// Returns the number of seconds this <see cref="TimeSpan"/> 
        /// is in the past relative to elapsed real time.
        /// </summary>
        public static float SecondsAgoRealTime(this TimeSpan time1)
        {
            return (float)(AssaultWingCore.Instance.GameTime.TotalRealTime - time1).TotalSeconds;
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
            return new Vector3(new Vector2(v.X, v.Y).Rotate(radians), v.Z);
        }

        /// <summary>
        /// Returns <paramref name="v"/> rotated around the positive Z axis 
        /// <paramref name="radians"/> radians.
        /// </summary>
        public static Vector2 Rotate(this Vector2 v, float radians)
        {
            var cosRadians = (float)Math.Cos(radians);
            var sinRadians = (float)Math.Sin(radians);
            return new Vector2(
                v.X * cosRadians - v.Y * sinRadians,
                v.Y * cosRadians + v.X * sinRadians);
        }

        public static TimeSpan Multiply(this TimeSpan time, int multiplier)
        {
            return TimeSpan.FromTicks(time.Ticks * multiplier);
        }

        public static TimeSpan Divide(this TimeSpan time, int divisor)
        {
            return TimeSpan.FromTicks(time.Ticks / divisor);
        }

        public static float Divide(this TimeSpan time, TimeSpan divisor)
        {
            return time.Ticks / (float)divisor.Ticks;
        }

        public static TimeSpan Min(TimeSpan time1, TimeSpan time2)
        {
            return new TimeSpan(Math.Min(time1.Ticks, time2.Ticks));
        }

        public static TimeSpan Max(TimeSpan time1, TimeSpan time2)
        {
            return new TimeSpan(Math.Max(time1.Ticks, time2.Ticks));
        }

        /// <summary>
        /// Returns the integral number of Assault Wing update frames this TimeSpan spans.
        /// </summary>
        public static int Frames(this TimeSpan time)
        {
            var ticksInFrame = AssaultWingCore.Instance.TargetElapsedTime.Ticks;
            return (int)((time.Ticks + ticksInFrame / 2) / ticksInFrame);
        }

        /// <summary>
        /// Returns the average of all values except the maximum and the minimum.
        /// </summary>
        public static TimeSpan AverageWithoutExtremes(IEnumerable<TimeSpan> times)
        {
            var ticks = times.Select(time => time.Ticks);
            if (ticks.Count() < 3) throw new ArgumentException("Need at least three values");
            return TimeSpan.FromTicks((ticks.Sum() - ticks.Min() - ticks.Max()) / (ticks.Count() - 2));
        }

        /// <summary>
        /// Returns the average of all values except the maximum and the minimum.
        /// </summary>
        public static int AverageWithoutExtremes(IEnumerable<int> values)
        {
            if (values.Count() < 3) throw new ArgumentException("Need at least three values");
            return (values.Sum() - values.Min() - values.Max()) / (values.Count() - 2);
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

        public static Vector2 MirrorY(this Vector2 value)
        {
            return new Vector2(value.X, -value.Y);
        }

        public static Vector2 ToVector2(this Point p)
        {
            return new Vector2(p.X, p.Y);
        }

        private static void Swap(ref Point point1, ref Point point2)
        {
            var swap = point1;
            point1 = point2;
            point2 = swap;
        }
    }
}
