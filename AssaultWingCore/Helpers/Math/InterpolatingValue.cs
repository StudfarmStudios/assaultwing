using System.Diagnostics;

namespace AW2.Helpers
{
    /// <summary>
    /// A value that interpolates towards a target with in constant steps.
    /// </summary>
    [DebuggerDisplay("{Current} +- {Step} -> {Target}")]
    public struct InterpolatingValue
    {
        /// <summary>
        /// The current value.
        /// </summary>
        public float Current { get; private set; }

        /// <summary>
        /// Interpolation step size, a positive value.
        /// </summary>
        public float Step { get; set; }

        /// <summary>
        /// The target value to interpolate towards.
        /// </summary>
        public float Target { get; set; }

        /// <summary>
        /// Is the value treated as an angle in radians, wrapping around every 2*pi.
        /// </summary>
        public bool AngularInterpolation { get; set; }

        /// <summary>
        /// Advances the current value by interpolating the next value.
        /// </summary>
        public void Advance()
        {
            if (AngularInterpolation)
                Current = AWMathHelper.InterpolateTowardsAngle(Current, Target, Step);
            else
                Current = AWMathHelper.InterpolateTowards(Current, Target, Step);
        }

        /// <summary>
        /// Resets the current value and target to a given value.
        /// </summary>
        public void Reset(float value)
        {
            Current = Target = value;
        }
    }
}
