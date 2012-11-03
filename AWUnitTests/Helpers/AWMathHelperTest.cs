using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Microsoft.Xna.Framework;

namespace AW2.Helpers
{
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

            public void PlotLine(int x, int y, int width)
            {
                while (width-- > 0)
                    Plot(x++, y);
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
            Assert.AreEqual(0, AWMathHelper.InterpolateTowards(0, 0, 0));
            Assert.AreEqual(0, AWMathHelper.InterpolateTowards(0, 0, 10));
            Assert.AreEqual(1, AWMathHelper.InterpolateTowards(0, 1, 10));
            Assert.AreEqual(-1, AWMathHelper.InterpolateTowards(0, -1, 10));
            Assert.AreEqual(10, AWMathHelper.InterpolateTowards(0, 11, 10));
            Assert.AreEqual(-10, AWMathHelper.InterpolateTowards(0, -11, 10));
        }

        [Test]
        public void TestInterpolateTowardsVector2()
        {
            var p1 = Vector2.Zero;
            var p2 = Vector2.UnitX;
            var p3 = Vector2.UnitY;
            var p4 = new Vector2(3, -4);
            Assert.AreEqual(p1, AWMathHelper.InterpolateTowards(p1, p1, 0));
            Assert.AreEqual(p1, AWMathHelper.InterpolateTowards(p1, p1, 10));
            Assert.AreEqual(p2, AWMathHelper.InterpolateTowards(p1, p2, 10));
            Assert.AreEqual(p3, AWMathHelper.InterpolateTowards(p1, p3, 10));
            Assert.AreEqual(p4, AWMathHelper.InterpolateTowards(p1, p4, 10));
            Assert.AreEqual(-p4, AWMathHelper.InterpolateTowards(p1, -p4, 10));
            Assert.AreEqual(10 * Vector2.UnitX, AWMathHelper.InterpolateTowards(p1, 11 * Vector2.UnitX, 10));
            Assert.AreEqual(-p4, AWMathHelper.InterpolateTowards(p1, -2 * p4, 5));
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

            // Trivial.
            AssertAngle(0, AWMathHelper.InterpolateTowardsAngle(0, 0, 0));

            // Trivial over 2*pi.
            AssertAngle(0, AWMathHelper.InterpolateTowardsAngle(MathHelper.TwoPi, MathHelper.TwoPi, MathHelper.TwoPi));

            // A) less; B) no; C) positive; D) no; E) positive; F) more
            AssertAngle(MathHelper.PiOver2, AWMathHelper.InterpolateTowardsAngle(0, MathHelper.Pi - 0.001f, MathHelper.PiOver2));

            // A) less; B) no; C) positive; D) no; E) positive; F) less
            AssertAngle(MathHelper.PiOver4, AWMathHelper.InterpolateTowardsAngle(0, MathHelper.PiOver4, MathHelper.PiOver2));

            // A) more; B) no; C) negative; D) no; E) positive; F) more
            AssertAngle(MathHelper.PiOver2, AWMathHelper.InterpolateTowardsAngle(MathHelper.Pi, 0.001f, MathHelper.PiOver2));

            // A) more; B) no; C) negative; D) no; E) positive; F) less
            AssertAngle(0, AWMathHelper.InterpolateTowardsAngle(MathHelper.PiOver4, 0, MathHelper.PiOver2));

            // A) more; B) yes; C) negative; D) yes; E) positive; F) more
            AssertAngle(-MathHelper.PiOver2, AWMathHelper.InterpolateTowardsAngle(0, -3 * MathHelper.PiOver4, MathHelper.PiOver2));

            // A) more; B) yes; C) negative; D) yes; E) positive; F) less
            AssertAngle(-3 * MathHelper.PiOver4, AWMathHelper.InterpolateTowardsAngle(0, -3 * MathHelper.PiOver4, MathHelper.Pi));

            // A) less; B) no; C) negative; D) yes; E) positive; F) more
            AssertAngle(-MathHelper.PiOver2, AWMathHelper.InterpolateTowardsAngle(0, 5 * MathHelper.PiOver4, MathHelper.PiOver2));

            // A) less; B) no; C) negative; D) yes; E) positive; F) less
            AssertAngle(-3 * MathHelper.PiOver4, AWMathHelper.InterpolateTowardsAngle(0, 5 * MathHelper.PiOver4, MathHelper.Pi));
        }

        [Test]
        public void TestGetAngleSpeedTowards()
        {
            var timeStep = TimeSpan.FromSeconds(0.5);
            AssertAngle(0, AWMathHelper.GetAngleSpeedTowards(0, 0, 0, timeStep));
            AssertAngle(MathHelper.PiOver4, AWMathHelper.GetAngleSpeedTowards(0, MathHelper.PiOver4, MathHelper.PiOver4, timeStep));
            AssertAngle(MathHelper.PiOver2, AWMathHelper.GetAngleSpeedTowards(0, MathHelper.PiOver4, MathHelper.Pi, timeStep));
            AssertAngle(-MathHelper.PiOver4, AWMathHelper.GetAngleSpeedTowards(0, -MathHelper.PiOver4, MathHelper.PiOver4, timeStep));
            AssertAngle(-MathHelper.PiOver2, AWMathHelper.GetAngleSpeedTowards(0, -MathHelper.PiOver4, MathHelper.Pi, timeStep));
            AssertAngle(MathHelper.PiOver4, AWMathHelper.GetAngleSpeedTowards(-MathHelper.PiOver4, MathHelper.PiOver4, MathHelper.PiOver4, timeStep));
            AssertAngle(MathHelper.Pi, AWMathHelper.GetAngleSpeedTowards(-MathHelper.PiOver4, MathHelper.PiOver4, MathHelper.TwoPi, timeStep));
            AssertAngle(MathHelper.PiOver4, AWMathHelper.GetAngleSpeedTowards(7 * MathHelper.PiOver4, MathHelper.PiOver4, MathHelper.PiOver4, timeStep));
            AssertAngle(MathHelper.Pi, AWMathHelper.GetAngleSpeedTowards(7 * MathHelper.PiOver4, MathHelper.PiOver4, MathHelper.TwoPi, timeStep));
        }

        private void AssertAngle(float expected, float actual)
        {
            var expectedNormalized = ((expected % MathHelper.TwoPi) + MathHelper.TwoPi) % MathHelper.TwoPi;
            var actualNormalized = ((actual % MathHelper.TwoPi) + MathHelper.TwoPi) % MathHelper.TwoPi;
            Assert.AreEqual(expectedNormalized, actualNormalized, 0.00001f);
        }

        [Test]
        public void TestAbsoluteAngleDifference()
        {
            Assert.AreEqual(0, AWMathHelper.AbsoluteAngleDifference(0, 0));
            Assert.AreEqual(1, AWMathHelper.AbsoluteAngleDifference(0, 1));
            Assert.AreEqual(1, AWMathHelper.AbsoluteAngleDifference(1, 0));
            Assert.AreEqual(1, AWMathHelper.AbsoluteAngleDifference(0, -1));
            Assert.AreEqual(1, AWMathHelper.AbsoluteAngleDifference(-1, 0));
            Assert.AreEqual(0, AWMathHelper.AbsoluteAngleDifference(0, MathHelper.TwoPi));
            Assert.AreEqual(0, AWMathHelper.AbsoluteAngleDifference(MathHelper.Pi, -MathHelper.Pi));
            Assert.AreEqual(MathHelper.Pi, AWMathHelper.AbsoluteAngleDifference(MathHelper.PiOver2, (float)(-2.5 * Math.PI)), 0.00001);
        }

        [Test]
        public void TestGetAbsoluteMinimalEqualAngle()
        {
            const double EPSILON = 0.0001;
            Assert.AreEqual(0, AWMathHelper.GetAbsoluteMinimalEqualAngle(0), EPSILON);
            Assert.AreEqual(0, AWMathHelper.GetAbsoluteMinimalEqualAngle(MathHelper.TwoPi), EPSILON);
            Assert.AreEqual(MathHelper.PiOver2, AWMathHelper.GetAbsoluteMinimalEqualAngle(MathHelper.PiOver2), EPSILON);
            Assert.AreEqual(-MathHelper.PiOver2, AWMathHelper.GetAbsoluteMinimalEqualAngle(-MathHelper.PiOver2), EPSILON);
            Assert.AreEqual(-MathHelper.PiOver2, AWMathHelper.GetAbsoluteMinimalEqualAngle(3 * MathHelper.PiOver2), EPSILON);
            Assert.AreEqual(MathHelper.PiOver2, AWMathHelper.GetAbsoluteMinimalEqualAngle(-3 * MathHelper.PiOver2), EPSILON);
            Assert.AreEqual(-MathHelper.Pi, AWMathHelper.GetAbsoluteMinimalEqualAngle(MathHelper.Pi), EPSILON);
            Assert.AreEqual(-MathHelper.Pi, AWMathHelper.GetAbsoluteMinimalEqualAngle(-MathHelper.Pi), EPSILON);
        }

        [Test]
        public void TestGetMinimalPositiveEqualAngle()
        {
            const double EPSILON = 0.0001;
            Assert.AreEqual(0, AWMathHelper.GetMinimalPositiveEqualAngle(0), EPSILON);
            Assert.AreEqual(0, AWMathHelper.GetMinimalPositiveEqualAngle(MathHelper.TwoPi), EPSILON);
            Assert.AreEqual(MathHelper.PiOver2, AWMathHelper.GetMinimalPositiveEqualAngle(MathHelper.PiOver2), EPSILON);
            Assert.AreEqual(3 * MathHelper.PiOver2, AWMathHelper.GetMinimalPositiveEqualAngle(-MathHelper.PiOver2), EPSILON);
            Assert.AreEqual(3 * MathHelper.PiOver2, AWMathHelper.GetMinimalPositiveEqualAngle(3 * MathHelper.PiOver2), EPSILON);
            Assert.AreEqual(MathHelper.PiOver2, AWMathHelper.GetMinimalPositiveEqualAngle(-3 * MathHelper.PiOver2), EPSILON);
            Assert.AreEqual(MathHelper.Pi, AWMathHelper.GetMinimalPositiveEqualAngle(MathHelper.Pi), EPSILON);
            Assert.AreEqual(MathHelper.Pi, AWMathHelper.GetMinimalPositiveEqualAngle(-MathHelper.Pi), EPSILON);
        }

        [Test]
        public void TestClampAngle()
        {
            Assert.AreEqual(0, AWMathHelper.ClampAngle(0, 0, 0));
            Assert.AreEqual(0, AWMathHelper.ClampAngle(0, 0, MathHelper.TwoPi));
            Assert.Throws<ArgumentException>(() => AWMathHelper.ClampAngle(0, MathHelper.TwoPi, 0));
            Assert.Throws<ArgumentException>(() => AWMathHelper.ClampAngle(0, 0, MathHelper.TwoPi + 0.1f));
            Assert.AreEqual(1, AWMathHelper.ClampAngle(0, 1, 2));
            Assert.AreEqual(2, AWMathHelper.ClampAngle(3, 1, 2));
            Assert.AreEqual(MathHelper.Pi, AWMathHelper.ClampAngle(-MathHelper.Pi, MathHelper.PiOver2, 3 * MathHelper.PiOver2));
            Assert.AreEqual(MathHelper.TwoPi, AWMathHelper.ClampAngle(0.1f, MathHelper.Pi, MathHelper.TwoPi));
        }

        [Test]
        public void TestRoundPowerTwo()
        {
            for (int power = 0; power < 31; ++power)
            {
                int n = 1 << power;
                Assert.AreEqual(n, AWMathHelper.CeilingPowerTwo(n));
                Assert.AreEqual(n, AWMathHelper.FloorPowerTwo(n));
            }
            Assert.AreEqual(4, AWMathHelper.CeilingPowerTwo(3));
            Assert.AreEqual(2, AWMathHelper.FloorPowerTwo(3));
            Assert.AreEqual(8, AWMathHelper.CeilingPowerTwo(7));
            Assert.AreEqual(4, AWMathHelper.FloorPowerTwo(7));
            Assert.AreEqual(16, AWMathHelper.CeilingPowerTwo(9));
            Assert.AreEqual(8, AWMathHelper.FloorPowerTwo(9));
            Assert.AreEqual(0x40000000, AWMathHelper.CeilingPowerTwo(0x3fffffff));
            Assert.AreEqual(0x20000000, AWMathHelper.FloorPowerTwo(0x3fffffff));
            Assert.AreEqual(0x40000000, AWMathHelper.FloorPowerTwo(0x50fa7e57));
        }

        [Test]
        public void TestLogTwo()
        {
            Assert.AreEqual(0, AWMathHelper.LogTwo(1));
            Assert.AreEqual(1, AWMathHelper.LogTwo(2));
            Assert.AreEqual(1, AWMathHelper.LogTwo(3));
            Assert.AreEqual(2, AWMathHelper.LogTwo(4));
            Assert.AreEqual(30, AWMathHelper.LogTwo(0x7fffffff));
            Assert.AreEqual(30, AWMathHelper.LogTwo(0x40000001));
            Assert.AreEqual(30, AWMathHelper.LogTwo(0x40000000));
            Assert.AreEqual(29, AWMathHelper.LogTwo(0x3fffffff));
            for (int i = 1; i < 0x40000000; i += i < 0x1234 ? 1 : 1301)
            {
                Assert.AreEqual(AWMathHelper.CeilingPowerTwo(i + 1), 1 << (1 + AWMathHelper.LogTwo(i)));
                Assert.AreEqual(AWMathHelper.FloorPowerTwo(i), 1 << AWMathHelper.LogTwo(i));
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
        public void TestAverageWithoutExtremes()
        {
            var t = TimeSpan.FromSeconds(1);
            var tBig = TimeSpan.FromSeconds(3);
            var tSmall = TimeSpan.FromSeconds(0.1);
            Assert.Throws<ArgumentException>(() => AWMathHelper.AverageWithoutExtremes(new TimeSpan[] { t, t }));
            Assert.AreEqual(t, AWMathHelper.AverageWithoutExtremes(new TimeSpan[] { t, t, t, t }));
            Assert.AreEqual(t, AWMathHelper.AverageWithoutExtremes(new TimeSpan[] { tBig, t, t, t }));
            Assert.AreEqual(t, AWMathHelper.AverageWithoutExtremes(new TimeSpan[] { t, tBig, t }));
            Assert.AreEqual(t, AWMathHelper.AverageWithoutExtremes(new TimeSpan[] { tSmall, t, t, t }));
            Assert.AreEqual(t, AWMathHelper.AverageWithoutExtremes(new TimeSpan[] { t, t, tSmall, }));
            Assert.AreEqual(t, AWMathHelper.AverageWithoutExtremes(new TimeSpan[] { t, tSmall, tBig }));
            Assert.AreEqual(tBig, AWMathHelper.AverageWithoutExtremes(new TimeSpan[] { tBig, tBig, tSmall, }));
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
            Assert.AreEqual(new Vector2(0, 0), AWMathHelper.Round(new Vector2(0, 0)));
            Assert.AreEqual(new Vector2(1, 1), AWMathHelper.Round(new Vector2(1.000001f, 1.499999f)));
            Assert.AreEqual(new Vector2(2, 2), AWMathHelper.Round(new Vector2(1.500001f, 1.999999f)));
            Assert.AreEqual(new Vector2(-1, -1), AWMathHelper.Round(new Vector2(-1.000001f, -1.499999f)));
            Assert.AreEqual(new Vector2(-2, -2), AWMathHelper.Round(new Vector2(-1.500001f, -1.999999f)));
            Assert.AreEqual(new Vector2(-1234567, 1234567), AWMathHelper.Round(new Vector2(-1234566.7f, 1234567.3f)));
        }

        [Test]
        public void TestAngle()
        {
            var DELTA = 0.000001;
            Assert.AreEqual(0, new Vector2(0, 0).Angle());
            Assert.AreEqual(0, new Vector2(0.1f, 0).Angle());
            Assert.AreEqual(0, new Vector2(1, 0).Angle());
            Assert.AreEqual(0, new Vector2(10, 0).Angle());
            Assert.AreEqual(MathHelper.PiOver2, new Vector2(0, 0.1f).Angle());
            Assert.AreEqual(MathHelper.PiOver2, new Vector2(0, 1).Angle());
            Assert.AreEqual(-MathHelper.PiOver2, new Vector2(0, -1).Angle());
            Assert.AreEqual(MathHelper.Pi, new Vector2(-1, 0).Angle());
            Assert.AreEqual(MathHelper.PiOver4, new Vector2(4, 4).Angle(), DELTA);
            Assert.AreEqual(3 * MathHelper.PiOver4, new Vector2(-4, 4).Angle(), DELTA);
            Assert.AreEqual(-3 * MathHelper.PiOver4, new Vector2(-4, -4).Angle(), DELTA);
            Assert.AreEqual(-MathHelper.PiOver4, new Vector2(4, -4).Angle(), DELTA);
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
        public void TestFloor()
        {
            Assert.AreEqual(2, 1.99999f.Floor());
            Assert.AreEqual(1, 1.9f.Floor());
            Assert.AreEqual(1, 1.5f.Floor());
            Assert.AreEqual(1, 1.1f.Floor());
            Assert.AreEqual(1, 1.00001f.Floor());
            Assert.AreEqual(1, 1f.Floor());
            Assert.AreEqual(1, 0.99999f.Floor());
            Assert.AreEqual(0, 0.9f.Floor());
            Assert.AreEqual(0, 0.5f.Floor());
            Assert.AreEqual(0, 0.1f.Floor());
            Assert.AreEqual(0, 0.00001f.Floor());
            Assert.AreEqual(0, 0f.Floor());
            Assert.AreEqual(0, -0.00001f.Floor());
            Assert.AreEqual(-1, (-0.1f).Floor());
            Assert.AreEqual(-1, (-0.5f).Floor());
            Assert.AreEqual(-1, (-0.9f).Floor());
            Assert.AreEqual(-1, (-0.99999f).Floor());
            Assert.AreEqual(-1, (-1f).Floor());
            Assert.AreEqual(-1, (-1.00001f).Floor());
            Assert.AreEqual(-2, (-1.1f).Floor());
            Assert.AreEqual(-2, (-1.5f).Floor());
            Assert.AreEqual(-2, (-1.9f).Floor());
            Assert.AreEqual(-2, (-1.99999f).Floor());
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
        public void TestRectangleClamp()
        {
            int width;
            int height;
            var rect = new Rectangle(50, 30, 150, 200);

            width = 0; height = 0;
            Assert.Throws<ArgumentException>(() => rect.Clamp(ref width, ref height));

            width = 100; height = 110;
            rect.Clamp(ref width, ref height);
            Assert.AreEqual(100, width);
            Assert.AreEqual(110, height);

            width = 300; height = 50;
            rect.Clamp(ref width, ref height);
            Assert.AreEqual(150, width);
            Assert.AreEqual(25, height);

            width = 60; height = 300;
            rect.Clamp(ref width, ref height);
            Assert.AreEqual(40, width);
            Assert.AreEqual(200, height);
        }

        [Test]
        public void TestRectangleStretch()
        {
            int width;
            int height;
            var rect = new Rectangle(50, 30, 150, 200);

            width = 0; height = 0;
            Assert.Throws<ArgumentException>(() => rect.Clamp(ref width, ref height));

            width = 100; height = 110;
            rect.Stretch(ref width, ref height);
            Assert.AreEqual(150, width);
            Assert.AreEqual(165, height);

            width = 300; height = 50;
            rect.Stretch(ref width, ref height);
            Assert.AreEqual(150, width);
            Assert.AreEqual(25, height);

            width = 60; height = 300;
            rect.Stretch(ref width, ref height);
            Assert.AreEqual(40, width);
            Assert.AreEqual(200, height);
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

        [Test]
        public void TestProjectOnto()
        {
            Assert.AreEqual(new Vector2(0, 0), new Vector2(0, 0).ProjectOnto(new Vector2(0, 0)));
            Assert.AreEqual(new Vector2(1, 0), new Vector2(1, 0).ProjectOnto(new Vector2(1, 0)));
            Assert.AreEqual(new Vector2(2, 0), new Vector2(2, 0).ProjectOnto(new Vector2(1, 0)));
            Assert.AreEqual(new Vector2(2, 0), new Vector2(2, 0).ProjectOnto(new Vector2(3, 0)));
            Assert.AreEqual(new Vector2(0, 0), new Vector2(1, 0).ProjectOnto(new Vector2(0, 1)));
            Assert.AreEqual(new Vector2(1, 0), new Vector2(1, 2).ProjectOnto(new Vector2(1, 0)));
            Assert.AreEqual(new Vector2(0, 2), new Vector2(1, 2).ProjectOnto(new Vector2(0, 1)));
            Assert.AreEqual(new Vector2(0, 1), new Vector2(0, 1).ProjectOnto(new Vector2(0, -1)));
            Assert.AreEqual(new Vector2(0, -1), new Vector2(0, -1).ProjectOnto(new Vector2(0, 1)));
            Assert.AreEqual(new Vector2(0, -1), new Vector2(0, -1).ProjectOnto(new Vector2(0, -1)));
            Assert.AreEqual(new Vector2(1, 1), new Vector2(0, 2).ProjectOnto(new Vector2(3, 3)));
        }

        private void DoFillTriangleTest(Point point1, Point point2, Point point3)
        {
            Console.Out.WriteLine(string.Format("DoFillTriangleTest({0}, {1}, {2})", point1, point2, point3));
            int xMin = Math.Min(Math.Min(point1.X, point2.X), point3.X);
            int yMin = Math.Min(Math.Min(point1.Y, point2.Y), point3.Y);
            int xMax = Math.Max(Math.Max(point1.X, point2.X), point3.X);
            int yMax = Math.Max(Math.Max(point1.Y, point2.Y), point3.Y);
            var data = new PlotData(xMax - xMin + 1, yMax - yMin + 1);
            AWMathHelper.FillTriangle(point1, point2, point3, (x, y) => data.Plot(x - xMin, y - yMin));
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
            AWMathHelper.FillCircle(x0, y0, radius, data.PlotLine);
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
}
