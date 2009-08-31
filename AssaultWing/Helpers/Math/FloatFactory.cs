using System;
using System.Collections.Generic;
using System.Text;

namespace AW2.Helpers
{
    /// <summary>
    /// Interface for structs producing float values from 
    /// initial parameters and input arguments.
    /// </summary>
    public interface FloatFactory
    {
        /// <summary>
        /// The number of arguments the float factory needs for producing a float.
        /// </summary>
        /// Calling <b>GetValue</b> on this instance is supported only with
        /// this argument count.
        /// <see cref="GetValue()"/>
        /// <see cref="GetValue(float)"/>
        int ArgumentCount { get; }

        /// <summary>
        /// Returns a value.
        /// </summary>
        /// <returns>The value.</returns>
        float GetValue();

        /// <summary>
        /// Returns a value based on an input arguments.
        /// </summary>
        /// <param name="input">The input arguments.</param>
        /// <returns>The value.</returns>
        float GetValue(float input);
    }
}
