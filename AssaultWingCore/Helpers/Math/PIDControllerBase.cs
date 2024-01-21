using System;

namespace AW2.Helpers
{
    /// <summary>
    /// Proportional-integral-derivative controller.
    /// <see cref="Output"/> is computed so that <see cref="Current"/> will get closer
    /// to <see cref="Target"/>. It is up to the caller to interpret Target, Current and
    /// Output in suitable ways.
    /// </summary>
    /// <seealso cref="http://en.wikipedia.org/wiki/PID_controller"/>
    public abstract class PIDControllerBase<T>
    {
        protected T _errorIntegral;
        protected T _errorDelta;
        protected T _previousError;

        public float ProportionalGain { get; set; }
        public float IntegralGain { get; set; }
        public float DerivativeGain { get; set; }
        public float OutputMaxAmplitude { get; set; }

        public Func<T> Target { get; private set; }
        public Func<T> Current { get; private set; }
        public T Output { get; protected set; }

        protected PIDControllerBase(Func<T> getTarget, Func<T> getCurrent)
        {
            Target = getTarget;
            Current = getCurrent;
            ProportionalGain = 0.07f;
            IntegralGain = 0.0005f;
            DerivativeGain = 0.01f;
            OutputMaxAmplitude = float.MaxValue;
        }

        public abstract void Compute();

        /// <summary>
        /// Resets the internal state to zero. To be used when <see cref="Current"/>
        /// is reset to <see cref="Target"/> by external means.
        /// </summary>
        public void Reset()
        {
            _previousError = _errorDelta = _errorIntegral = default(T);
            Output = default(T);
        }
    }
}
