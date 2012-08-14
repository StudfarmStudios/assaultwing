using System;

namespace AW2.Helpers
{
    public class LazyProxy<TData, TValue> where TValue : class
    {
        /// <summary>
        /// If <see cref="_getValue"/> returns null, it will be called again on every
        /// <see cref="NULL_WAIT_RETRY_COUNT"/>'th call to <see cref="GetValue"/>.
        /// This is an optimization so that not too much time is spent in <see cref="_getValue"/>.
        /// </summary>
        public const int NULL_WAIT_RETRY_COUNT = 30;

        private Func<TData, TValue> _getValue;
        private TData _data;
        private TValue _value;
        private bool _hasData;
        private bool _hasValue;
        private int _nullRetryCount;

        public LazyProxy(Func<TData, TValue> getValue)
        {
            _getValue = getValue;
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
            if (_hasValue) return _value;
            if (!_hasData) return null;
            if (_nullRetryCount-- > 0) return null;
            _nullRetryCount = NULL_WAIT_RETRY_COUNT;
            _value = _getValue(_data);
            _hasValue = _value != null;
            return _value;
        }
    }
}
