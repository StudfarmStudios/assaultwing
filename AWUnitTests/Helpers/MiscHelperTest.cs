using NUnit.Framework;
using System;

namespace AW2.Helpers
{
    [TestFixture]
    public class MiscHelperTest
    {
        [Test]
        public void TestCapitalize()
        {
            Assert.AreEqual("Testing", "testing".Capitalize());
            Assert.AreEqual("", "".Capitalize());
            Assert.AreEqual("Testing", "Testing".Capitalize());
            Assert.AreEqual("TESTING", "TESTING".Capitalize());
            Assert.AreEqual("Testing 123 testing", "Testing 123 testing".Capitalize());
            Assert.AreEqual(" testing", " testing".Capitalize());
            Assert.AreEqual("!\"#¤%&/()=", "!\"#¤%&/()=".Capitalize());
        }

        [Test]
        public void TestCapitalizeWords()
        {
            Assert.AreEqual("Testing", "testing".CapitalizeWords());
            Assert.AreEqual("", "".CapitalizeWords());
            Assert.AreEqual("Testing", "Testing".CapitalizeWords());
            Assert.AreEqual("TESTING", "TESTING".CapitalizeWords());
            Assert.AreEqual("Testing 123 Testing", "Testing 123 testing".CapitalizeWords());
            Assert.AreEqual(" Testing", " testing".CapitalizeWords());
            Assert.AreEqual("!\"#¤%&/()=", "!\"#¤%&/()=".Capitalize());
            Assert.AreEqual("Testing  Testing", "Testing  testing".CapitalizeWords());
            Assert.AreEqual("Testing-testing", "Testing-testing".CapitalizeWords());
        }

        [Test]
        public void TestToDurationString()
        {
            Assert.Throws<ArgumentException>(() => TimeSpan.FromMinutes(-1).ToDurationString(null, "hour", "minute", "second", true));
            Assert.AreEqual("", TimeSpan.Zero.ToDurationString(null, "hour", "minute", "second", true));
            Assert.AreEqual("1 second", TimeSpan.FromSeconds(1).ToDurationString(null, "hour", "minute", "second", true));
            Assert.AreEqual("1 second", TimeSpan.FromSeconds(1.5).ToDurationString(null, "hour", "minute", "second", true));
            Assert.AreEqual("2 seconds", TimeSpan.FromSeconds(2).ToDurationString(null, "hour", "minute", "second", true));
            Assert.AreEqual("10 seconds", TimeSpan.FromSeconds(10).ToDurationString(null, "hour", "minute", "second", true));
            Assert.AreEqual("1 minute", TimeSpan.FromSeconds(60).ToDurationString(null, "hour", "minute", "second", true));
            Assert.AreEqual("1 minute 1 second", TimeSpan.FromSeconds(61).ToDurationString(null, "hour", "minute", "second", true));
            Assert.AreEqual("1 hour 2 seconds", new TimeSpan(1, 0, 2).ToDurationString(null, "hour", "minute", "second", true));
            Assert.AreEqual("2 hours 3 minutes", new TimeSpan(2, 3, 0).ToDurationString(null, "hour", "minute", "second", true));
            Assert.AreEqual("25 hours 59 minutes 59 seconds", new TimeSpan(25, 59, 59).ToDurationString(null, "hour", "minute", "second", true));
            Assert.AreEqual("2 d", new TimeSpan(2, 0, 0, 30).ToDurationString("d", "h", "min", null, false));
            Assert.AreEqual("3 h 15 min", new TimeSpan(0, 3, 15, 0).ToDurationString("d", "h", "min", null, false));
            Assert.AreEqual("", TimeSpan.FromMinutes(15).ToDurationString(null, null, null, null, true));
            Assert.AreEqual("", TimeSpan.FromMinutes(15).ToDurationString(null, "hour", null, null, true));
            Assert.AreEqual("900 seconds", TimeSpan.FromMinutes(15).ToDurationString(null, null, null, "second", true));
            Assert.AreEqual("2 days 900 seconds", new TimeSpan(2, 0, 15, 0).ToDurationString("day", null, null, "second", true));
        }

        [Test]
        public void TestToOrdinalString()
        {
            Assert.AreEqual("0th", 0.ToOrdinalString());
            Assert.AreEqual("1st", 1.ToOrdinalString());
            Assert.AreEqual("2nd", 2.ToOrdinalString());
            Assert.AreEqual("3rd", 3.ToOrdinalString());
            Assert.AreEqual("4th", 4.ToOrdinalString());
            Assert.AreEqual("9th", 9.ToOrdinalString());
            Assert.AreEqual("10th", 10.ToOrdinalString());
            Assert.AreEqual("11th", 11.ToOrdinalString());
            Assert.AreEqual("12th", 12.ToOrdinalString());
            Assert.AreEqual("13th", 13.ToOrdinalString());
            Assert.AreEqual("19th", 19.ToOrdinalString());
            Assert.AreEqual("20th", 20.ToOrdinalString());
            Assert.AreEqual("21st", 21.ToOrdinalString());
            Assert.AreEqual("22nd", 22.ToOrdinalString());
            Assert.AreEqual("23rd", 23.ToOrdinalString());
            Assert.AreEqual("24th", 24.ToOrdinalString());
            Assert.AreEqual("99th", 99.ToOrdinalString());
            Assert.AreEqual("100th", 100.ToOrdinalString());
            Assert.AreEqual("101st", 101.ToOrdinalString());
            Assert.AreEqual("102nd", 102.ToOrdinalString());
            Assert.AreEqual("103rd", 103.ToOrdinalString());
            Assert.AreEqual("104th", 104.ToOrdinalString());
            Assert.AreEqual("6662nd", 6662.ToOrdinalString());
            Assert.AreEqual("100001st", 100001.ToOrdinalString());
            Assert.Throws<ArgumentOutOfRangeException>(() => (-1).ToOrdinalString());
        }

        [Test]
        public void TestFirstDifference()
        {
            object a, b;
            int index;
            Assert.False(MiscHelper.FirstDifference(new object[] { 2, 3, 4 }, new object[] { 2, 3, 4 }, out a, out b, out index));
            Assert.Null(a);
            Assert.Null(b);
            Assert.AreEqual(-1, index);
            Assert.False(MiscHelper.FirstDifference(new object[] { null, null }, new object[] { null, null }, out a, out b, out index));
            Assert.Null(a);
            Assert.Null(b);
            Assert.AreEqual(-1, index);
            Assert.False(MiscHelper.FirstDifference(new object[0], new object[0], out a, out b, out index));
            Assert.Null(a);
            Assert.Null(b);
            Assert.AreEqual(-1, index);
            Assert.True(MiscHelper.FirstDifference(new object[] { 1, 3, 4 }, new object[] { 2, 3, 4 }, out a, out b, out index));
            Assert.AreEqual(1, a);
            Assert.AreEqual(2, b);
            Assert.AreEqual(0, index);
            Assert.True(MiscHelper.FirstDifference(new object[] { 1, 3, 4 }, new object[] { 1, 3, 5 }, out a, out b, out index));
            Assert.AreEqual(4, a);
            Assert.AreEqual(5, b);
            Assert.AreEqual(2, index);
            Assert.True(MiscHelper.FirstDifference(new object[] { 1, 3, 4 }, new object[] { 1, null, 4 }, out a, out b, out index));
            Assert.AreEqual(3, a);
            Assert.AreEqual(null, b);
            Assert.AreEqual(1, index);
            Assert.True(MiscHelper.FirstDifference(new object[] { 1, 3, 4 }, new object[] { 1, 3 }, out a, out b, out index));
            Assert.AreEqual(4, a);
            Assert.Null(b);
            Assert.AreEqual(2, index);
            Assert.True(MiscHelper.FirstDifference(new object[0], new object[] { 1 }, out a, out b, out index));
            Assert.Null(a);
            Assert.AreEqual(1, b);
            Assert.AreEqual(0, index);
        }

        [Test]
        public void TestBytesToString()
        {
            var bytes = new byte[] { 0, 1, 2, 3, 0xff, 40, 0x80, 0x7f };
            var firstFour = MiscHelper.BytesToString(new ArraySegment<byte>(bytes, 0, 4));
            Assert.AreEqual("00,01,02,03", firstFour);
            var lastFour = MiscHelper.BytesToString(new ArraySegment<byte>(bytes, 4, 4));
            Assert.AreEqual("FF,28,80,7F", lastFour);
        }
    }
}
