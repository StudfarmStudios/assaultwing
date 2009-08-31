using System;
using System.Collections.Generic;
using System.Text;

namespace AW2.Helpers
{
    /// <summary>
    /// A constant value. Wraps System.Single in the FloatFactory interface.
    /// </summary>
    /// Code adapted from a ConstantValue example in the C# Language Specification version 1.2,
    /// copyright Microsoft Corporation.
    public struct ConstantValue : FloatFactory
    {
        float value;

        /// <summary>
        /// Private instance constructor.
        /// </summary>
        /// <param name="value">The value.</param>
        ConstantValue(float value)
        {
            this.value = value;
        }

        /// <summary>
        /// Implicit conversion from float to ConstantValue.
        /// </summary>
        public static implicit operator ConstantValue(float x)
        {
            return new ConstantValue(x);
        }

        /// <summary>
        /// Implicit conversion from ConstantValue to float.
        /// </summary>
        public static implicit operator float(ConstantValue x)
        {
            return x.value;
        }

        /// <summary>
        /// Unary addition.
        /// </summary>
        public static ConstantValue operator +(ConstantValue x)
        {
            return x;
        }

        /// <summary>
        /// Unary negation.
        /// </summary>
        public static ConstantValue operator -(ConstantValue x)
        {
            return -x.value;
        }

        /// <summary>
        /// Binary addition.
        /// </summary>
        public static ConstantValue operator +(ConstantValue x, ConstantValue y)
        {
            return x.value + y.value;
        }

        /// <summary>
        /// Subtraction.
        /// </summary>
        public static ConstantValue operator -(ConstantValue x, ConstantValue y)
        {
            return x.value - y.value;
        }

        /// <summary>
        /// Multiplication.
        /// </summary>
        public static ConstantValue operator *(ConstantValue x, ConstantValue y)
        {
            return x.value * y.value;
        }

        /// <summary>
        /// Division.
        /// </summary>
        public static ConstantValue operator /(ConstantValue x, ConstantValue y)
        {
            return x.value / y.value;
        }

        /// <summary>
        /// Modulus operator.
        /// </summary>
        public static ConstantValue operator %(ConstantValue x, ConstantValue y)
        {
            return x.value % y.value;
        }

        /// <summary>
        /// Equality operator.
        /// </summary>
        public static bool operator ==(ConstantValue x, ConstantValue y)
        {
            return x.value == y.value;
        }

        /// <summary>
        /// Inequality operator.
        /// </summary>
        public static bool operator !=(ConstantValue x, ConstantValue y)
        {
            return x.value != y.value;
        }

        /// <summary>
        /// Greater than operator.
        /// </summary>
        public static bool operator >(ConstantValue x, ConstantValue y)
        {
            return x.value > y.value;
        }

        /// <summary>
        /// Less than operator.
        /// </summary>
        public static bool operator <(ConstantValue x, ConstantValue y)
        {
            return x.value < y.value;
        }

        /// <summary>
        /// Greater or equal to operator.
        /// </summary>
        public static bool operator >=(ConstantValue x, ConstantValue y)
        {
            return x.value >= y.value;
        }

        /// <summary>
        /// Less or equal to operator.
        /// </summary>
        public static bool operator <=(ConstantValue x, ConstantValue y)
        {
            return x.value <= y.value;
        }

        /// <summary>
        /// Value equality with an object.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (!(obj is ConstantValue)) return false;
            ConstantValue x = (ConstantValue)obj;
            return value == x.value;
        }

        /// <summary>
        /// Returns a hash code for the value.
        /// </summary>
        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        /// <summary>
        /// Returns a string representation of the value.
        /// </summary>
        public override string ToString()
        {
            return value.ToString();
        }

        #region FloatFactory Members

        /// <summary>
        /// The number of arguments the float factory needs for producing a float.
        /// </summary>
        /// Calling <b>GetValue</b> on this instance is supported only with
        /// this argument count.
        /// <see cref="GetValue()"/>
        /// <see cref="GetValue(float)"/>
        public int ArgumentCount { get { return 0; } }

        /// <summary>
        /// Returns a value.
        /// </summary>
        /// <returns>The value.</returns>
        public float GetValue()
        {
            return value;
        }

        /// <summary>
        /// Returns a value based on an input arguments.
        /// </summary>
        /// <param name="input">The input arguments.</param>
        /// <returns>The value.</returns>
        public float GetValue(float input)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #endregion
    }
}