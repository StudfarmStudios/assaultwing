using System;
using NUnit.Framework;

namespace AW2.Helpers.Serialization
{
    [TestFixture]
    public class ChangingStateTest
    {
        [Test]
        public void TestAll()
        {
            var t1 = TimeSpan.FromSeconds(1);
            var t2 = TimeSpan.FromSeconds(2);
            var t3 = TimeSpan.FromSeconds(3);
            var state = new ChangingState<int>();

            Assert.AreEqual(false, state.HasChanged);
            Assert.Throws<InvalidOperationException>(() => Ignore(state.State));

            state.Set(42, t1);
            Assert.AreEqual(true, state.HasChanged);
            Assert.AreEqual(42, state.State);
            Assert.AreEqual(true, state.HasChanged); // many reads all get the same results
            Assert.AreEqual(42, state.State);

            state.Set(69, t1);
            Assert.AreEqual(true, state.HasChanged);
            Assert.AreEqual(69, state.State);

            state.Set(69, t2); // time changes but value doesn't
            Assert.AreEqual(false, state.HasChanged);
            Assert.AreEqual(69, state.State);

            state.Set(101, t2);
            Assert.AreEqual(true, state.HasChanged);
            Assert.AreEqual(101, state.State);

            state.Set(142, t3);
            Assert.AreEqual(true, state.HasChanged);
            state.Set(101, t3); // value is the same as at previous time
            Assert.AreEqual(false, state.HasChanged);
        }

        private void Ignore(int x) { }
    }
}
