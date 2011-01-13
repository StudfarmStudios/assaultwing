using System;
using System.Collections.Generic;
using System.Linq;

namespace AW2.Helpers.Serialization
{
    /// <summary>
    /// Provides functionality to notice changes in a state variable that is,
    /// during each frame update, first written to (possibly many times) and then
    /// read from (possibly many times).
    /// </summary>
    public class ChangingState<T>
    {
        private enum InitType { None, OnlyCurrent, Full };

        private InitType _init;
        private TimeSpan _currentTime;
        private TimeSpan _previousTime;
        private T _currentState;
        private T _previousState;

        public bool HasChanged
        {
            get
            {
                if (_init == InitType.None) return false;
                if (_init == InitType.OnlyCurrent) return true;
                return !_currentState.Equals(_previousState);
            }
        }

        public T State
        {
            get
            {
                if (_init == InitType.None) throw new InvalidOperationException("State not initialized");
                return _currentState;
            }
        }

        public ChangingState()
        {
            _init = InitType.None;
        }

        public void Set(T state, TimeSpan time)
        {
            if (time == _currentTime)
                _currentState = state;
            else
            {
                _previousState = _currentState;
                _previousTime = _currentTime;
                _currentState = state;
                _currentTime = time;
                _init = _init == InitType.None ? InitType.OnlyCurrent : InitType.Full;
            }
        }
    }
}
