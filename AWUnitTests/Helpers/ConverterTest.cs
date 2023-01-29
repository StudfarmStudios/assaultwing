using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace AW2.Helpers
{
    [TestFixture]
    public class ConverterTest
    {
        [Test]
        public void TestFloatToIntToFloat()
        {
            Assert.AreEqual(0, Converter.IntToFloat(0));
            Assert.AreEqual(0, Converter.FloatToInt(0));
            var testValues = new[]
            {
                0,
                1,
                -1,
                1234.5678e-12f,
                -9876.5432e-12f,
                1234.5678e12f,
                -9876.5432e12f,
                float.MaxValue,
                float.MinValue,
                float.Epsilon,
                float.PositiveInfinity,
                float.NegativeInfinity,
                float.NaN,
            };
            foreach (var x in testValues) Assert.AreEqual(x, Converter.IntToFloat(Converter.FloatToInt(x)));
        }

        [Test]
        public void TestHalfToShortToHalf()
        {
            Assert.AreEqual(Half.Zero, Converter.ShortToHalf(0));
            Assert.AreEqual((short)0, Converter.HalfToShort(Half.Zero));
            var testValues = new[]
            {
                Half.Zero,
                new Half(1),
                new Half(-1),
                new Half(1234.5678e-3f),
                new Half(-9876.5432e-3f),
                new Half(1234.5678e3f),
                new Half(-9876.5432e3f),
                Half.MaxValue,
                Half.MinValue,
                Half.Epsilon,
                Half.PositiveInfinity,
                Half.NegativeInfinity,
                Half.NaN,
            };
            foreach (var x in testValues) Assert.AreEqual(x, Converter.ShortToHalf(Converter.HalfToShort(x)));
        }
    }
}
