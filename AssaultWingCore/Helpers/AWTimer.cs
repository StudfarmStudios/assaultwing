using System;

namespace AW2.Helpers
{
    /// <summary>
    /// Measures regular intervals in game time. The current interval may be set to some
    /// irregular length. Missed IsElaped moments stack up by default; <see cref="IsElapsed"/>
    /// will return true as many times as there are past intervals since the previous call to
    /// IsElapsed. Set <see cref="SkipPastIntervals"/> to true to make the next interval
    /// start from the current time each time IsElapsed returns true.
    /// </summary>
    public class AWTimer
    {
        private Func<TimeSpan> _getTime;
        private TimeSpan _regularInterval;
        private TimeSpan _currentInterval;
        private TimeSpan _currentStart;

        public bool SkipPastIntervals { get; set; }
        private bool IsIntervalFinished { get { return _getTime() >= _currentStart + _currentInterval; } }

        /// <summary>
        /// Returns true if the current frame is the first after the set interval has
        /// passed since the last time IsElapsed was true.
        /// </summary>
        public bool IsElapsed
        {
            get
            {
                if (!IsIntervalFinished) return false;
                _currentStart = SkipPastIntervals ? _getTime() : _currentStart + _currentInterval;
                _currentInterval = _regularInterval;
                return true;
            }
        }

        public AWTimer(Func<TimeSpan> getTime, TimeSpan interval)
        {
            if (interval <= TimeSpan.Zero) throw new ArgumentOutOfRangeException("interval");
            _getTime = getTime;
            _currentInterval = _regularInterval = interval;
            _currentStart = _getTime();
        }

        public void SetCurrentInterval(TimeSpan interval)
        {
            _currentInterval = interval;
        }

        public override string ToString()
        {
            return string.Format("{0} + {1}{2}", _currentStart, _currentInterval,
                _currentInterval == _regularInterval ? "" : " (" + _regularInterval + ")");
        }
    }
}
