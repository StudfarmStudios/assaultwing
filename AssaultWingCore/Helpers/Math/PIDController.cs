using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace AW2.Helpers
{
    /// <summary>
    /// Proportional-integral-derivative controller for <see cref="Vector2"/> values.
    /// <see cref="Output"/> is computed so that <see cref="Current"/> will get closer
    /// to <see cref="Target"/>. It is up to the caller to interpret Target, Current and
    /// Output in suitable ways.
    /// </summary>
    /// <seealso cref="http://en.wikipedia.org/wiki/PID_controller"/>
    public class PIDController
    {
        private Vector2 _errorIntegral;
        private Vector2 _errorDelta;
        private Vector2 _previousError;

        public float ProportionalGain { get; set; }
        public float IntegralGain { get; set; }
        public float DerivativeGain { get; set; }
        public float OutputMaxLength { get; set; }

        public Func<Vector2> Target { get; private set; }
        public Func<Vector2> Current { get; private set; }
        public Vector2 Output { get; private set; }

        public PIDController(Func<Vector2> getTarget, Func<Vector2> getCurrent)
        {
            Target = getTarget;
            Current = getCurrent;
            ProportionalGain = 0.07f;
            IntegralGain = 0.0005f;
            DerivativeGain = 0.01f;
            OutputMaxLength = float.MaxValue;
        }

        public void Compute()
        {
            var error = Target() - Current();
            _errorIntegral += error;
            _errorDelta = error - _previousError;
            _previousError = error;
            Output = (ProportionalGain * error +
                IntegralGain * _errorIntegral +
                DerivativeGain * _errorDelta).Clamp(0, OutputMaxLength);
        }

        /// <summary>
        /// Resets the internal state to zero. To be used when <see cref="Current"/>
        /// is reset to <see cref="Target"/> by external means.
        /// </summary>
        public void Reset()
        {
            _previousError = _errorDelta = _errorIntegral = Vector2.Zero;
            Output = Vector2.Zero;
        }
    }
}
