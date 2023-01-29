using System;
using System.Diagnostics;

namespace AW2.Helpers
{
    /// <summary>
    /// A 16-bit floating point number. See IEEE 754r.
    /// </summary>
    /// A 16-bit, or half-precision, floating point value has precision
    /// of 3 significant digits. Value range is -65504...65504.
    public struct Half
    {
        public static readonly Half MaxValue = new Half(HALF_MAX_VALUE);
        public static readonly Half MinValue = new Half(HALF_MIN_VALUE);
        public static readonly Half Zero = new Half(0);
        public static readonly Half Epsilon = new Half(HALF_SMALLEST_POSITIVE_NORMAL_VALUE);
        public static readonly Half PositiveInfinity = new Half(float.PositiveInfinity);
        public static readonly Half NegativeInfinity = new Half(float.NegativeInfinity);
        public static readonly Half NaN = new Half(float.NaN);

        private const float HALF_MAX_VALUE = 65504;
        private const float HALF_MIN_VALUE = -65504;
        private const float HALF_SMALLEST_POSITIVE_NORMAL_VALUE = 0.000061035156f;

        /// <summary>
        /// Bitwise correct contents of the half-precision floating point value.
        /// </summary>
        private int _value; // Note: Profiling shows that int is 15 times faster here than short.

        /// <summary>
        /// Creates a Half with a known value.
        /// </summary>
        public Half(float x)
        {
            if (float.IsPositiveInfinity(x) || x > HALF_MAX_VALUE)
                _value = 0x7c00; // positive infinity and positive overflow
            else if (float.IsNegativeInfinity(x) || x < HALF_MIN_VALUE)
                _value = 0xfc00; // negative infinity and negative overflow
            else if (float.IsNaN(x))
                _value = 0x7e00; // not a number
            else if (x == 0f ||
                (x > 0f && x < HALF_SMALLEST_POSITIVE_NORMAL_VALUE) ||
                (x < 0f && x > -HALF_SMALLEST_POSITIVE_NORMAL_VALUE))
                _value = 0x0000; // negative zero, positive zero, negative underflow and positive underflow
            else // a regular number
            {
                int single = Converter.FloatToInt(x);

                // Decode the bit representations of the components of the 32-bit float.
                // Bits as stated in IEEE 754: 1 + 8 + 23 (sign + exponent + significand)
                int sign = (single >> 31) & 0x1;
                int exponent = (single >> 23) & 0xff;
                int significand = single & 0x7fffff; // without the implicit bit

                // Find out bit representations of the components of the 16-bit float.
                // Bits as stated in IEEE 754r: 1 + 5 + 10 (sign + exponent + significand)
                Debug.Assert(exponent - 127 <= 15, "Positive overflow");
                Debug.Assert(exponent - 127 >= -14, "Negative overflow");
                var halfExponent = exponent - 127 + 15;
                var halfSignificand = significand >> (23 - 10);

                // Construct the 16-bit representation in native byte order.
                _value = (ushort)((sign << 15) | (halfExponent << 10) | halfSignificand);
            }
        }

        /// <summary>
        /// Implicit conversion from Half to Single.
        /// </summary>
        /// <param name="x">The Half value.</param>
        /// <returns>The equivalent Single value.</returns>
        public static implicit operator float(Half x)
        {
            if (x._value == 0x7c00) return float.PositiveInfinity;
            if (x._value == 0xfc00) return float.NegativeInfinity;
            if (x._value == 0x7e00) return float.NaN;
            if (x._value == 0) return 0;

            // Decode bit representations of the components of the 16-bit float.
            // Bits as stated in IEEE 754r: 1 + 5 + 10 (sign + exponent + significand)
            int sign = (x._value >> 15) & 0x1;
            int exponent = (x._value >> 10) & 0x1f;
            int significand = x._value & 0x3ff; // without the implicit bit

            // Construct the 32-bit representation in native byte order.
            // Bits as stated in IEEE 754: 1 + 8 + 23 (sign + exponent + significand)
            int singleExponent = exponent - 15 + 127;
            int singleSignificand = significand << (23 - 10);
            int single = (sign << 31) | (singleExponent << 23) | singleSignificand;

            return Converter.IntToFloat(single);
        }

        /// <summary>
        /// Explicit conversion from Single to Half.
        /// </summary>
        /// <param name="x">The Single value.</param>
        /// <returns>The best matching Half value.</returns>
        public static explicit operator Half(float x)
        {
            return new Half(x);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Half)) return false;
            return _value == ((Half)obj)._value;
        }

        public override int GetHashCode()
        {
            return _value;
        }

        public override string ToString()
        {
            return ((float)this).ToString();
        }
    }
}
