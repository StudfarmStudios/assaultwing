using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;

namespace AW2.Helpers
{
    /// <summary>
    /// A parametrised random variable. Both its expected value and
    /// variance are functions of a float argument. Variance means
    /// the maximum absolute difference of any value of the random
    /// variable from the expected value of the random variable.
    /// </summary>
    public struct ExpectedValueCurve : FloatFactory
    {
        Curve expected;
        Curve variance;

        /// <summary>
        /// Creates a random variable with a expected value and variance functions.
        /// </summary>
        /// <param name="expected">The function of expected value.</param>
        /// <param name="variance">The function of variance.</param>
        public ExpectedValueCurve(Curve expected, Curve variance)
        {
            this.expected = expected;
            this.variance = variance;
        }

        /// <summary>
        /// The function of expected value.
        /// </summary>
        public Curve Expected { get { return expected; } set { expected = value; } }

        /// <summary>
        /// The function of variance.
        /// </summary>
        public Curve Variance { get { return variance; } set { variance = value; } }

        #region FloatFactory Members

        /// <summary>
        /// The number of arguments the float factory needs for producing a float.
        /// </summary>
        /// Calling <b>GetValue</b> on this instance is supported only with
        /// this argument count.
        /// <see cref="GetValue()"/>
        /// <see cref="GetValue(float)"/>
        public int ArgumentCount { get { return 1; } }

        /// <summary>
        /// Returns a value.
        /// </summary>
        /// <returns>The value.</returns>
        public float GetValue()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        /// <summary>
        /// Returns a value based on an input arguments.
        /// </summary>
        /// <param name="input">The input arguments.</param>
        /// <returns>The value.</returns>
        public float GetValue(float input)
        {
            float expectedNow = expected.Evaluate(input);
            float varianceNow = variance.Evaluate(input);
            return expectedNow + varianceNow * RandomHelper.GetRandomFloat(-1, 1);
        }

        #endregion
    }
}
