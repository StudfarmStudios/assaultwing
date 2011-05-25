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
            Assert.Throws<ArgumentException>(() => TimeSpan.FromMinutes(-1).ToDurationString());
            Assert.AreEqual("0 seconds", TimeSpan.Zero.ToDurationString());
            Assert.AreEqual("1 second", TimeSpan.FromSeconds(1).ToDurationString());
            Assert.AreEqual("1 second", TimeSpan.FromSeconds(1.5).ToDurationString());
            Assert.AreEqual("2 seconds", TimeSpan.FromSeconds(2).ToDurationString());
            Assert.AreEqual("10 seconds", TimeSpan.FromSeconds(10).ToDurationString());
            Assert.AreEqual("1 minute", TimeSpan.FromSeconds(60).ToDurationString());
            Assert.AreEqual("1 minute 1 second", TimeSpan.FromSeconds(61).ToDurationString());
            Assert.AreEqual("1 hour 2 seconds", new TimeSpan(1, 0, 2).ToDurationString());
            Assert.AreEqual("2 hours 3 minutes", new TimeSpan(2, 3, 0).ToDurationString());
            Assert.AreEqual("25 hours 59 minutes 59 seconds", new TimeSpan(25, 59, 59).ToDurationString());
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
