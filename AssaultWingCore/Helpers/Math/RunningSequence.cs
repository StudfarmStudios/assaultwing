using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace AW2.Helpers
{
    /// <summary>
    /// Keeps track of the latest values in a running sequence and provides a few 
    /// characteristic properties of them. Is safe to add values from different threads.
    /// </summary>
    public class RunningSequence<T>
    {
        private struct ValueData
        {
            public T Value;
            public TimeSpan EntryTime;
        }

        private ConcurrentQueue<ValueData> _values = new ConcurrentQueue<ValueData>();
        private TimeSpan _valueTimeout;
        private Func<IEnumerable<T>, T> _sum;
        private Func<T, float, T> _divide;
        private TimeSpan _lastEntryTime;

        public int Count { get { return _values.Count; } }
        public T Min { get { return RawValues.Any() ? RawValues.Min() : default(T); } }
        public T Max { get { return RawValues.Any() ? RawValues.Max() : default(T); } }
        public T Sum { get { return _sum(RawValues); } }
        public T Average { get { return RawValues.Any() ? _divide(Sum, Count) : default(T); } }

        private IEnumerable<T> RawValues { get { return _values.Select(x => x.Value); } }

        public RunningSequence(TimeSpan valueTimeout, Func<IEnumerable<T>, T> sum, Func<T, float, T> divide)
        {
            _valueTimeout = valueTimeout;
            _sum = sum;
            _divide = divide;
        }

        /// <summary>
        /// <paramref name="entryTime"/> is assumed to be non-decreasing in subsequent calls.
        /// Failure to obey this rule leads to more or less inaccurate output values.
        /// </summary>
        public void Add(T value, TimeSpan entryTime)
        {
            _lastEntryTime = entryTime;
            _values.Enqueue(new ValueData { Value = value, EntryTime = entryTime });
        }

        /// <summary>
        /// Prunes out old values. Call this method every time before reading output values.
        /// Call this method only from one thread at a time. Returns self.
        /// </summary>
        public RunningSequence<T> Prune(TimeSpan now)
        {
            ValueData valueData;
            while (_values.TryPeek(out valueData) && valueData.EntryTime + _valueTimeout <= now)
                _values.TryDequeue(out valueData);
            return this;
        }

        /// <summary>
        /// Returns a clone of this RunningSequence. Useful if this RunningSequence is being
        /// added to from other threads and you want to read various output values that
        /// represent one consistent state.
        /// </summary>
        public RunningSequence<T> Clone()
        {
            var clone = new RunningSequence<T>(_valueTimeout, _sum, _divide);
            clone._values = new ConcurrentQueue<ValueData>(_values);
            return clone;
        }
    }

    public class RunningSequenceTimeSpan : RunningSequence<TimeSpan>
    {
        public RunningSequenceTimeSpan(TimeSpan valueTimeout)
            : base(valueTimeout,
                timeSpans => new TimeSpan((long)timeSpans.Select(x => x.Ticks).Sum()),
                (timeSpan, divisor) => new TimeSpan((long)(timeSpan.Ticks / divisor)))
        {
        }
    }

    public class RunningSequenceSingle : RunningSequence<float>
    {
        public RunningSequenceSingle(TimeSpan valueTimeout)
            : base(valueTimeout, Enumerable.Sum, (x, divisor) => x / divisor)
        {
        }
    }
}
