using System;
using System.Collections.Generic;
using System.Linq;

namespace AW2.Helpers
{
    /// <summary>
    /// Keeps track of the latest values in a running sequence and
    /// provides a few characteristic properties of them.
    /// </summary>
    public class RunningSequence<T>
    {
        private struct ValueData
        {
            public T Value;
            public TimeSpan EntryTime;
        }

        private Queue<ValueData> _values = new Queue<ValueData>();
        private TimeSpan _valueTimeout;
        private Func<IEnumerable<T>, T> _sum;
        private Func<T, float, T> _divide;
        private TimeSpan _lastEntryTime;

        public T Min { get { return RawValues.Min(); } }
        public T Max { get { return RawValues.Max(); } }
        public T Sum { get { return _sum(RawValues); } }
        public T Average { get { return _divide(Sum, RawValues.Count()); } }

        private IEnumerable<T> RawValues { get { return _values.Select(x => x.Value); } }

        public RunningSequence(TimeSpan valueTimeout, Func<IEnumerable<T>, T> sum, Func<T, float, T> divide)
        {
            _valueTimeout = valueTimeout;
            _sum = sum;
            _divide = divide;
        }

        public void Add(T value, TimeSpan entryTime)
        {
            if (_values.Any() && entryTime < _lastEntryTime) throw new InvalidOperationException("EntryTimes must not decrease");
            _lastEntryTime = entryTime;
            _values.Enqueue(new ValueData { Value = value, EntryTime = entryTime });
            while (_values.Peek().EntryTime + _valueTimeout <= entryTime) _values.Dequeue();
        }
    }
}
