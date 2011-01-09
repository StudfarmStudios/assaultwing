using System;
using NUnit.Framework;

namespace AW2.Helpers
{
    [TestFixture]
    public class LazyProxyTest
    {
        [Test]
        public void TestLazyInt()
        {
            var proxy = new LazyProxy<int, int>(x => Tuple.Create(true, 2 * x), -1);
            Assert.AreEqual(-1, proxy.GetValue());
            proxy.SetData(3);
            Assert.AreEqual(6, proxy.GetValue());
            Assert.Throws<InvalidOperationException>(() => proxy.SetData(42));
        }

        [Test]
        public void TestLazyString()
        {
            var proxy = new LazyProxy<int, string>(x => Tuple.Create(true, new System.Text.StringBuilder().Append('x', x).ToString()));
            Assert.AreEqual(null, proxy.GetValue());
            proxy.SetData(5);
            string proxyValue = proxy;
            Assert.AreEqual("xxxxx", proxyValue);
        }

        [Test]
        public void TestEager()
        {
            var proxy = new LazyProxy<int, int>(6);
            Assert.AreEqual(6, proxy.GetValue());
            Assert.Throws<InvalidOperationException>(() => proxy.SetData(42));
        }

        [Test]
        public void TestUnavailableValue()
        {
            int importantNumber = 42;
            var proxy = new LazyProxy<int, int>(
                x => importantNumber < 50
                    ? Tuple.Create(false, 0)
                    : Tuple.Create(true, 2 * x));
            Assert.AreEqual(0, proxy.GetValue());
            proxy.SetData(3);
            Assert.AreEqual(0, proxy.GetValue());
            importantNumber = 69;
            Assert.AreEqual(6, proxy.GetValue());
            importantNumber = 0; // doesn't affect GetValue any more
            Assert.AreEqual(6, proxy.GetValue());
        }
    }
}
