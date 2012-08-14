using System;
using NUnit.Framework;

namespace AW2.Helpers
{
    [TestFixture]
    public class LazyProxyTest
    {
        [Test]
        public void TestSetDataTwice()
        {
            var proxy = new LazyProxy<int, string>(x => new System.Text.StringBuilder().Append('x', x).ToString());
            proxy.SetData(5);
            Assert.Throws<InvalidOperationException>(() => proxy.SetData(6));
        }

        [Test]
        public void TestLazyString()
        {
            var proxy = new LazyProxy<int, string>(x => new System.Text.StringBuilder().Append('x', x).ToString());
            Assert.AreEqual(null, proxy.GetValue());
            proxy.SetData(5);
            string proxyValue = proxy;
            Assert.AreEqual("xxxxx", proxyValue);
        }

        [Test]
        public void TestEager()
        {
            var proxy = new LazyProxy<int, string>("6");
            Assert.AreEqual("6", proxy.GetValue());
            Assert.Throws<InvalidOperationException>(() => proxy.SetData(42));
        }

        [Test]
        public void TestUnavailableValue()
        {
            var funcCalled = false;
            Action<Action> assertThatFuncIsCalled = action =>
            {
                Assert.IsFalse(funcCalled);
                action();
                Assert.IsTrue(funcCalled);
                funcCalled = false;
            };

            var importantNumber = 42;
            var proxy = new LazyProxy<int, string>(x =>
            {
                funcCalled = true;
                return importantNumber < 50 ? null : x.ToString();
            });
            Assert.AreEqual(null, proxy.GetValue());
            proxy.SetData(3);
            assertThatFuncIsCalled(() => Assert.AreEqual(null, proxy.GetValue()));
            for (int i = 0; i < LazyProxy<int, string>.NULL_WAIT_RETRY_COUNT; i++)
                Assert.AreEqual(null, proxy.GetValue());
            importantNumber = 69;
            assertThatFuncIsCalled(() => Assert.AreEqual("3", proxy.GetValue()));
            importantNumber = 0; // doesn't affect GetValue any more
            Assert.AreEqual("3", proxy.GetValue());
            Assert.IsFalse(funcCalled);
        }
    }
}
