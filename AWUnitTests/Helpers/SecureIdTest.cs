using System;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;

namespace AW2.Helpers
{
    [TestFixture]
    public class SecureIdTest
    {
        [Test]
        public void TestStable()
        {
            var secureId = new SecureId();
            Assert.AreEqual(secureId.MakeId("foo"), secureId.MakeId("foo"));
            Assert.AreEqual(secureId.MakeId("bar"), secureId.MakeId("bar"));
            Assert.AreNotEqual(secureId.MakeId("foo"), secureId.MakeId("bar"));
        }

        [Test]
        public void TestLength()
        {
            var secureId = new SecureId();
            Assert.AreEqual(24, secureId.MakeId("foo").Length);
        }

        [Test]
        public void TestSalted()
        {
            var secureId1 = new SecureId();
            var secureId2 = new SecureId();
            Assert.AreNotEqual(secureId1.MakeId("foo"), secureId2.MakeId("foo"));
        }

        [Test]
        public void TestSpeed()
        {
            var secureId = new SecureId();
            int accumulator = 0;
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            var count = 10000;
            foreach (var i in Enumerable.Range(0, count))
            {
                accumulator += secureId.MakeId($"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa {i}").Length;
            }
            stopWatch.Stop();
            // This be somethingmuch less than 1ms. Since the server is single threaded it could stutter
            // when a player joins if the SecureId is adjusted to be too slow.
            Assert.Less(((float)stopWatch.ElapsedMilliseconds) / count, 0.1f);
            Assert.AreEqual(count * 24, accumulator);
        }
    }
}
