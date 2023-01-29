using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace AW2.Graphics
{
    [TestFixture]
    public class WrappedTextTest
    {
        private Func<string, float> _getStringWidth;

        [SetUp]
        public void Setup()
        {
            _getStringWidth = x => x.Sum(ch => ch == 'i' ? 2.2f : ch == 'W' ? 11 : 5.5f);
        }

        [Test]
        public void TestEmptyLine()
        {
            var text = new WrappedText("", _getStringWidth);
            var lines = text.WrapToWidth(20).ToArray();
            Assert.AreEqual(0, lines.Length);
        }

        [Test]
        public void TestFitsOnOneLine()
        {
            var text = new WrappedText("foo", _getStringWidth);
            var lines = text.WrapToWidth(100).ToArray();
            Assert.AreEqual(1, lines.Length);
            Assert.AreEqual("foo", lines[0]);
        }

        [Test]
        public void TestForcedWrap()
        {
            var text = new WrappedText("foofoo", _getStringWidth);
            var lines20 = text.WrapToWidth(20).ToArray();
            Assert.AreEqual(1, lines20.Length);
            Assert.AreEqual("foofoo", lines20[0]);
        }

        [Test]
        public void TestWrapAtSpace()
        {
            var niceText = new WrappedText("foo foo foo foo foo", _getStringWidth);
            var niceLines = niceText.WrapToWidth(40).ToArray();
            Assert.AreEqual(3, niceLines.Length);
            Assert.AreEqual("foo foo", niceLines[0]);
            Assert.AreEqual("foo foo", niceLines[1]);
            Assert.AreEqual("foo", niceLines[2]);
            var longSpaceText = new WrappedText("   foo   foo   foo         ", _getStringWidth);
            var longSpaceLines = longSpaceText.WrapToWidth(49.5f).ToArray();
            Assert.AreEqual(2, longSpaceLines.Length);
            Assert.AreEqual("foo   foo", longSpaceLines[0]);
            Assert.AreEqual("foo", longSpaceLines[1].TrimEnd());
        }

        [Test]
        public void TestNarrowChars()
        {
            var text = new WrappedText("iii iii xxx xxx", _getStringWidth);
            var lines = text.WrapToWidth(19).ToArray();
            Assert.AreEqual(3, lines.Length);
            Assert.AreEqual("iii iii", lines[0]);
            Assert.AreEqual("xxx", lines[1]);
            Assert.AreEqual("xxx", lines[2]);
            var longText = new WrappedText("iii iii iii iii iii", _getStringWidth);
            var longLines = longText.WrapToWidth(55).ToArray();
            Assert.AreEqual(1, longLines.Length);
            Assert.AreEqual("iii iii iii iii iii", longLines[0]);
        }

        [Test]
        public void TestWideChars()
        {
            var text = new WrappedText("WWW WWW WWW WWW", _getStringWidth);
            var lines = text.WrapToWidth(71.5f).ToArray();
            Assert.AreEqual(2, lines.Length);
            Assert.AreEqual("WWW WWW", lines[0]);
            Assert.AreEqual("WWW WWW", lines[1]);
        }
    }
}
