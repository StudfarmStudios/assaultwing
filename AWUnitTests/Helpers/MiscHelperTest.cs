using System;
using System.Collections.Specialized;
using System.Linq;
using NUnit.Framework;

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
        public void TestParseQueryString()
        {
            NameValueCollectionsEqual(new NameValueCollection(), MiscHelper.ParseQueryString("?"));
            var expected = new NameValueCollection();
            expected.Add("key", "value");
            NameValueCollectionsEqual(expected, MiscHelper.ParseQueryString("?key=value"));
            expected = new NameValueCollection();
            expected.Add("key", "value,other value");
            NameValueCollectionsEqual(expected, MiscHelper.ParseQueryString("?key=value&key=other%20value"));
            expected = new NameValueCollection();
            expected.Add("+09?", "?1§2?");
            expected.Add("/foo?", "?other v/ää/ue?");
            NameValueCollectionsEqual(expected, MiscHelper.ParseQueryString("?\t+09?  = ?1§2? & %2ffoo? = ?other v%2f%c3%A4%c3%A4%2Fue? "));
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

        private void NameValueCollectionsEqual(NameValueCollection a, NameValueCollection b)
        {
            Assert.AreEqual(a.Count, b.Count, "NameValueCollections are of different size");
            var aKeys = a.Keys.Cast<string>().OrderBy(aKey => aKey);
            var bKeys = b.Keys.Cast<string>().OrderBy(bKey => bKey);
            var aValues = aKeys.Select(aKey => a[aKey]);
            var bValues = bKeys.Select(bKey => b[bKey]);
            Assert.AreEqual(aKeys.ToArray(), bKeys.ToArray(), "NameValuecollection keys differ");
            Assert.AreEqual(aValues.ToArray(), bValues.ToArray(), "NameValuecollection values differ");
        }
    }
}
