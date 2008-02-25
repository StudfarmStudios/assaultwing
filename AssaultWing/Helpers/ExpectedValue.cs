using System;
using System.Collections.Generic;
using System.Text;

namespace AW2.Helpers
{
    /// <summary>
    /// Represents a random variable with a given expected value and variance.
    /// </summary>
    public struct ExpectedValue
    {
        private float expected;
        private float variance;

        /// <summary>
        /// Creates a random variable with a given expected value and variance
        /// </summary>
        /// <param name="expected">Expected value returned by GetRandomValue()</param>
        /// <param name="variance">Absolute variance of values returned by GetRandomValue()</param>
        public ExpectedValue(float expected, float variance)
        {
            this.expected = expected;
            this.variance = variance;
        }

        /// <summary>
        /// Expected value returned by GetRandomValue()
        /// </summary>
        public float Expected
        {
            get { return expected; }
            set { expected = value; }
        }

        /// <summary>
        /// Absolute variance of values returned by GetRandomValue()
        /// </summary>
        public float Variance
        {
            get { return variance; }
            set { variance = (value > 0 ? value : 0); } // only accept positive variance
        }

        /// <summary>
        /// Returns a value with a given expected value and variance of +/-Variance
        /// </summary>
        /// <returns></returns>
        public float GetRandomValue()
        {
            if (variance == 0f) return expected;
            return expected - variance + (2 * variance * RandomHelper.GetRandomFloat());
        }
    }
}
