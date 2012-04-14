using System;
using NUnit.Framework;

namespace AW2.Helpers
{
    [TestFixture]
    public class AWTimerTest
    {
        private TimeSpan Time { get; set; }
        private TimeSpan GetTime() { return Time; }
        private AWTimer Timer { get; set; }

        private void AssertIsElapsed(bool expectedIsElapsed, double timeSeconds)
        {
            Time = TimeSpan.FromSeconds(timeSeconds);
            Assert.AreEqual(expectedIsElapsed, Timer.IsElapsed);
        }

        [SetUp]
        public void Setup()
        {
            Time = TimeSpan.Zero;
            Timer = new AWTimer(GetTime, TimeSpan.FromSeconds(1));
        }

        [Test]
        public void TestInvalidArgument()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new AWTimer(GetTime, TimeSpan.FromSeconds(0)));
            Assert.Throws<ArgumentOutOfRangeException>(() => new AWTimer(GetTime, TimeSpan.FromSeconds(-1)));
        }

        [Test]
        public void TestElapseOnlyOnceAfterInterval()
        {
            AssertIsElapsed(true, 1);
            AssertIsElapsed(false, 1);
        }

        [Test]
        public void TestRegularInterval()
        {
            AssertIsElapsed(false, 0);
            AssertIsElapsed(false, 0.99);
            AssertIsElapsed(true, 1);
            AssertIsElapsed(false, 1.5);
        }

        [Test]
        public void TestSetLongInterval()
        {
            Timer.SetCurrentInterval(TimeSpan.FromSeconds(10));
            AssertIsElapsed(false, 0);
            AssertIsElapsed(false, 9);
            AssertIsElapsed(true, 10);
            AssertIsElapsed(false, 10);
        }

        [Test]
        public void TestSetShortInterval()
        {
            Timer.SetCurrentInterval(TimeSpan.FromSeconds(0.1));
            AssertIsElapsed(false, 0);
            AssertIsElapsed(true, 0.1);
            AssertIsElapsed(false, 0.1);
        }

        [Test]
        public void TestDontSkipIntervals()
        {
            AssertIsElapsed(true, 2);
            AssertIsElapsed(true, 2);
            AssertIsElapsed(false, 2);
        }
    }
}
