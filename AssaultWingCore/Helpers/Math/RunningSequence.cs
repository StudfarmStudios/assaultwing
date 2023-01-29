using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace AW2.Helpers
{
    public class RunningSequenceTimeSpan
    {
        private RunningSequenceSingle _ticks;

        public int Count { get { return _ticks.Count; } }
        public TimeSpan Min { get { return TimeSpan.FromTicks((long)_ticks.Min); } }
        public TimeSpan Max { get { return TimeSpan.FromTicks((long)_ticks.Max); } }
        public TimeSpan Sum { get { return TimeSpan.FromTicks((long)_ticks.Sum); } }
        public TimeSpan Average { get { return TimeSpan.FromTicks((long)_ticks.Average); } }

        public RunningSequenceTimeSpan(TimeSpan valueTimeout)
        {
            _ticks = new RunningSequenceSingle(valueTimeout);
        }

        private RunningSequenceTimeSpan(RunningSequenceSingle ticks)
        {
            _ticks = ticks;
        }

        public void Add(TimeSpan value, TimeSpan entryTime)
        {
            _ticks.Add(value.Ticks, entryTime);
        }

        public RunningSequenceTimeSpan Prune(TimeSpan now)
        {
            _ticks.Prune(now);
            return this;
        }

        public RunningSequenceTimeSpan Clone()
        {
            return new RunningSequenceTimeSpan(_ticks.Clone());
        }
    }

    /// <summary>
    /// Keeps track of the latest values in a running sequence and provides a few 
    /// characteristic properties of them. Is safe to add values from different threads.
    /// </summary>
    public class RunningSequenceSingle
    {
        [System.Diagnostics.DebuggerDisplay("{Value} @ {EntryTime}")]
        private struct ValueData
        {
            public float Value;
            public TimeSpan EntryTime;
        }

        private Queue<ValueData> _values = new Queue<ValueData>();
        private TimeSpan _valueTimeout;
        private TimeSpan _lastEntryTime;

        public int Count { get { return _values.Count; } }
        public float Min { get; private set; }
        public float Max { get; private set; }
        public float Sum { get; private set; }
        public float Average { get { return Count == 0 ? 0 : Sum / Count; } }

        public RunningSequenceSingle(TimeSpan valueTimeout)
        {
            _valueTimeout = valueTimeout;
        }

        /// <summary>
        /// <paramref name="entryTime"/> is assumed to be non-decreasing in subsequent calls.
        /// Failure to obey this rule leads to more or less inaccurate output values.
        /// </summary>
        public void Add(float value, TimeSpan entryTime)
        {
            lock (_values)
            {
                _lastEntryTime = entryTime;
                _values.Enqueue(new ValueData { Value = value, EntryTime = entryTime });
                if (value < Min || Count == 1) Min = value;
                if (value > Max || Count == 1) Max = value;
                Sum += value;
            }
        }

        /// <summary>
        /// Prunes out old values. Call this method every time before reading output values.
        /// Call this method only from one thread at a time. Returns self.
        /// </summary>
        public RunningSequenceSingle Prune(TimeSpan now)
        {
            lock (_values)
            {
                while (_values.Any() && _values.Peek().EntryTime + _valueTimeout <= now) _values.Dequeue();
                UpdateProperties();
                return this;
            }
        }

        /// <summary>
        /// Returns a clone of this RunningSequence. Useful if this RunningSequence is being
        /// added to from other threads and you want to read various output values that
        /// represent one consistent state.
        /// </summary>
        public RunningSequenceSingle Clone()
        {
            lock (_values)
            {
                var clone = new RunningSequenceSingle(_valueTimeout);
                clone._values = new Queue<ValueData>(_values);
                clone.Min = Min;
                clone.Max = Max;
                clone.Sum = Sum;
                return clone;
            }
        }

        private void UpdateProperties()
        {
            if (!_values.Any())
            {
                Min = Max = Sum = 0;
                return;
            }
            Min = float.MaxValue;
            Max = float.MinValue;
            Sum = 0;
            foreach (var value in _values)
            {
                var valueValue = value.Value;
                if (valueValue < Min) Min = valueValue;
                if (valueValue > Max) Max = valueValue;
                Sum += valueValue;
            }
        }
    }
}
