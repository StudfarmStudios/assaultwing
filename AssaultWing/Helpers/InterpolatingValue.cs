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
        /// Advances the current value by interpolating the next value.
        /// </summary>
        public void Advance()
        {
            Current = AWMathHelper.InterpolateTowards(Current, Target, Step);
        }
    }
}
