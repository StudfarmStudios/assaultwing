using System;
using NUnit.Framework;

namespace AW2.Helpers
{
    [TestFixture]
    public class RunningSequenceTest
    {
        private RunningSequenceSingle _seq;

        [SetUp]
        public void Setup()
        {
            _seq = new RunningSequenceSingle(TimeSpan.FromSeconds(4));
        }

        [Test]
        public void TestConstructor()
        {
            Assert.AreEqual(0, _seq.Count);
            AssertCountMinMaxAverage(0, 0, 0, 0);
        }

        [Test]
        public void TestAdd()
        {
            _seq.Add(42, TimeSpan.FromSeconds(1));
            AssertCountMinMaxAverage(1, 42, 42, 42);
            _seq.Add(22, TimeSpan.FromSeconds(2));
            AssertCountMinMaxAverage(2, 22, 42, 32);
            _seq.Add(92, TimeSpan.FromSeconds(3));
            AssertCountMinMaxAverage(3, 22, 92, 52);
        }

        [Test]
        public void TestPrune()
        {
            _seq.Prune(TimeSpan.FromSeconds(0));
            AssertCountMinMaxAverage(0, 0, 0, 0);
            _seq.Add(1, TimeSpan.FromSeconds(1));
            _seq.Add(2, TimeSpan.FromSeconds(2));
            _seq.Add(3, TimeSpan.FromSeconds(3));
            _seq.Prune(TimeSpan.FromSeconds(3));
            AssertCountMinMaxAverage(3, 1, 3, 2);
            _seq.Add(4, TimeSpan.FromSeconds(4));
            _seq.Add(5, TimeSpan.FromSeconds(5));
            _seq.Prune(TimeSpan.FromSeconds(5.5));
            AssertCountMinMaxAverage(4, 2, 5, 3.5f);
            _seq.Prune(TimeSpan.FromSeconds(5.5));
            AssertCountMinMaxAverage(4, 2, 5, 3.5f);
        }

        [Test]
        public void TestClone()
        {
            _seq.Add(1, TimeSpan.FromSeconds(1));
            _seq.Add(2, TimeSpan.FromSeconds(2));
            _seq.Add(3, TimeSpan.FromSeconds(3));
            AssertCountMinMaxAverage(3, 1, 3, 2);
            var seq2 = _seq.Clone();
            AssertCountMinMaxAverage(3, 1, 3, 2, seq2);
            _seq.Add(4, TimeSpan.FromSeconds(4));
            AssertCountMinMaxAverage(3, 1, 3, 2, seq2);
            AssertCountMinMaxAverage(4, 1, 4, 2.5f);
        }

        private void AssertCountMinMaxAverage(int count, float min, float max, float average, RunningSequenceSingle seq = null)
        {
            if (seq == null) seq = _seq;
            Assert.AreEqual(count, seq.Count);
            Assert.AreEqual(min, seq.Min);
            Assert.AreEqual(max, seq.Max);
            Assert.AreEqual(average, seq.Average);
        }
    }
}
