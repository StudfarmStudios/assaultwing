using System;
using Microsoft.Xna.Framework;

namespace AW2.Helpers
{
    /// <summary>
    /// Proportional-integral-derivative controller for <see cref="Microsoft.Xna.Framework.Vector2"/> values.
    /// </summary>
    public class PIDController2 :PIDControllerBase<Vector2>
    {
        public PIDController2(Func<Vector2> getTarget, Func<Vector2> getCurrent)
            : base(getTarget, getCurrent)
        {
        }

        public override void Compute()
        {
            var error = Target() - Current();
            _errorIntegral += error;
            _errorDelta = error - _previousError;
            _previousError = error;
            Output = (ProportionalGain * error +
                IntegralGain * _errorIntegral +
                DerivativeGain * _errorDelta).Clamp(0, OutputMaxAmplitude);
        }
    }
}
