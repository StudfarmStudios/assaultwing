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
}
