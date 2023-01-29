using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace AW2.Helpers
{
    /// <summary>
    /// Proportional-integral-derivative controller for <see cref="System.Single"/> values.
    /// </summary>
    public class PIDController : PIDControllerBase<float>
    {
        public PIDController(Func<float> getTarget, Func<float> getCurrent)
            : base(getTarget, getCurrent)
        {
        }

        public override void Compute()
        {
            var error = Target() - Current();
            _errorIntegral += error;
            _errorDelta = error - _previousError;
            _previousError = error;
            Output = MathHelper.Clamp(
                ProportionalGain * error +
                IntegralGain * _errorIntegral +
                DerivativeGain * _errorDelta,
                -OutputMaxAmplitude, OutputMaxAmplitude);
        }
    }
}
