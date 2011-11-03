using System;

namespace AW2.Game.GobUtils
{
    /// <summary>
    /// An action that happens at a specified time.
    /// </summary>
    public class TimedAction
    {
        private TimeSpan _interval;
        private Action _action;
        private TimeSpan _nextAction;

        public TimedAction(TimeSpan interval, Action action)
        {
            _interval = interval;
            _action = action;
        }

        public void Update(TimeSpan totalTime)
        {
            if (totalTime < _nextAction) return;
            _nextAction = totalTime + _interval;
            _action();
        }
    }
}
