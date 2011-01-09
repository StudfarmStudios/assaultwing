﻿using NUnit.Framework;

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
    }
}
