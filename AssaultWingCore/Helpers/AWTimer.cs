using System;
using AW2.Core;

namespace AW2.Helpers
{
    /// <summary>
    /// Measures regular intervals in game time. The current interval may be set to some
    /// irregular length. Missed IsElaped moments do not stack up; if IsElapsed is checked
    /// after two or more intervals have passed, the next interval starts from the current
    /// time.
    /// </summary>
    public class AWTimer
    {
        private AssaultWingCore _game;
        private TimeSpan _regularInterval;
        private TimeSpan _currentInterval;
        private TimeSpan _currentElapsed;
        private TimeSpan _currentStart;

        private TimeSpan Now { get { return _game.GameTime.TotalGameTime; } }
        private bool IsIntervalFinished { get { return Now >= _currentStart + _currentInterval; } }

        /// <summary>
        /// Returns true if the current frame is the first after the set interval has
        /// passed since the last time IsElapsed was true.
        /// </summary>
        public bool IsElapsed
        {
            get
            {
                if (IsIntervalFinished)
                {
                    _currentElapsed = Now;
                    _currentInterval = _regularInterval;
                    _currentStart = IsIntervalFinished ? Now : _currentStart + _currentInterval;
                }
                return Now == _currentElapsed;
            }
        }

        public AWTimer(AssaultWingCore game, TimeSpan interval)
        {
            _game = game;
            _currentInterval = _regularInterval = interval;
            _currentStart = _game.GameTime.TotalGameTime + _currentInterval;
        }

        public void SetCurrentInterval(TimeSpan interval)
        {
            _currentInterval = interval;
        }
    }
}
