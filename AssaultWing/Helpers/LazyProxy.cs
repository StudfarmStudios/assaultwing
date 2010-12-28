#if DEBUG
using NUnit.Framework;
#endif
using System;

namespace AW2.Helpers
{
    public class LazyProxy<TData, TValue>
    {
        private Func<TData, Tuple<bool, TValue>> _getValue;
        private TData _data;
        private TValue _value;
        private TValue _defaultValue;
        private bool _hasData;
        private bool _hasValue;

        /// <param name="getValue">Returns the value for given data.
        /// Returns (false, _) if value wasn't available.
        /// Otherwise returns (true, value).</param>
        public LazyProxy(Func<TData, Tuple<bool, TValue>> getValue, TValue defaultValue = default(TValue))
        {
            _getValue = getValue;
            _defaultValue = defaultValue;
        }

        public LazyProxy(TValue value)
        {
            _value = value;
            _hasValue = true;
        }

        public static implicit operator LazyProxy<TData, TValue>(TValue value)
        {
            return new LazyProxy<TData, TValue>(value);
        }

        public static implicit operator TValue(LazyProxy<TData, TValue> proxy)
        {
            return proxy.GetValue();
        }

        public void SetData(TData data)
        {
            if (_hasData) throw new InvalidOperationException("Data is already set");
            if (_hasValue) throw new InvalidOperationException("Value is already set");
            _data = data;
            _hasData = true;
        }

        public TValue GetValue()
        {
            if (!_hasValue)
            {
                if (!_hasData) return _defaultValue;
                var result = _getValue(_data);
                if (!result.Item1) return _defaultValue;
                _value = result.Item2;
                _hasValue = true;
            }
            return _value;
        }
    }

#if DEBUG
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
#endif
}
