using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace AW2.UI
{
    [TestFixture]
    public class CharacterSetTest
    {
        [Test]
        public void TestEmpty()
        {
            var set = new CharacterSet(new char[0]);
            Assert.False(set.Contains(' '));
            Assert.False(set.Contains('\x0'));
            Assert.False(set.Contains('\x9f93'));
            Assert.False(set.Contains('ä'));
        }

        [Test]
        public void TestSingletonRange()
        {
            var set = new CharacterSet(new[] { 'y' });
            Assert.False(set.Contains('x'));
            Assert.True(set.Contains('y'));
            Assert.False(set.Contains('z'));
        }

        [Test]
        public void TestProperRange()
        {
            var set = new CharacterSet(Enumerable.Range(0, 20).Select(x => (char)('b' + x)));
            Assert.False(set.Contains('a'));
            Assert.True(set.Contains('b'));
            Assert.True(set.Contains((char)('b' + 19)));
            Assert.False(set.Contains((char)('b' + 20)));
        }

        [Test]
        public void TestProperAndSingletonRanges()
        {
            var set = new CharacterSet(Enumerable.Range(0, 3).Select(x => (char)('b' + x)).Union(
                new[] { '5' }));
            Assert.False(set.Contains('a'));
            Assert.True(set.Contains('b'));
            Assert.True(set.Contains('c'));
            Assert.True(set.Contains('d'));
            Assert.False(set.Contains('e'));
            Assert.False(set.Contains('4'));
            Assert.True(set.Contains('5'));
            Assert.False(set.Contains('6'));
        }

        [Test]
        public void TestTwoProperRanges()
        {
            var set = new CharacterSet(Enumerable.Range(0, 3).Select(x => (char)('b' + x)).Union(
                Enumerable.Range(0, 5).Select(x => (char)('2' + x))));
            Assert.False(set.Contains('a'));
            Assert.True(set.Contains('b'));
            Assert.True(set.Contains('c'));
            Assert.True(set.Contains('d'));
            Assert.False(set.Contains('e'));
            Assert.False(set.Contains('1'));
            Assert.True(set.Contains('2'));
            Assert.True(set.Contains('6'));
            Assert.False(set.Contains('7'));
        }
    }
}
