using System;

namespace AW2.Helpers
{
    /// <summary>
    /// A 16-bit floating point number. See IEEE 754r.
    /// </summary>
    /// A 16-bit, or half-precision, floating point value has precision
    /// of 3 significant digits. Value range is -65504...65504.
    public struct Half
    {
        /// <summary>
        /// Bitwise correct contents of the half-precision floating point value.
        /// </summary>
        ushort value;

        /// <summary>
        /// The greatest Half.
        /// </summary>
        public static readonly Half MaxValue = new Half(65504);

        /// <summary>
        /// The least Half.
        /// </summary>
        public static readonly Half MinValue = new Half(-65504);

        /// <summary>
        /// The bit representation of the Half.
        /// </summary>
        public ushort BitRepresentation { get { return value; } set { this.value = value; } }

        /// <summary>
        /// Creates a Half with a known value.
        /// </summary>
        /// <param name="x">The value.</param>
        public Half(float x)
        {
            if (float.IsPositiveInfinity(x) || x > 65504)
                value = 0x7c00; // positive infinity and positive overflow
            else if (float.IsNegativeInfinity(x) || x < -65504)
                value = 0xfc00; // negative infinity and negative overflow
            else if (float.IsNaN(x))
                value = 0x7e00; // not a number
            else if (x == 0f ||
                (x > 0f && x < 0.000061035156f) ||
                (x < 0f && x > -0.000061035156f))
                value = 0x0000; // negative zero, positive zero, negative underflow and positive underflow
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
                int halfExponent = 0;
                if (exponent - 127 > 15) // should always be false; caught above as infinity
                    halfExponent = 30;
                else if (exponent - 127 < -14) // should always be false; caught above as underflow
                    halfExponent = 1;
                else
                    halfExponent = exponent - 127 + 15;
                int halfSignificand = significand >> (23 - 10);

                // Construct the 16-bit representation in native byte order.
                value = (ushort)((sign << 15) | (halfExponent << 10) | halfSignificand);
            }
        }

        /// <summary>
        /// Implicit conversion from Half to Single.
        /// </summary>
        /// <param name="x">The Half value.</param>
        /// <returns>The equivalent Single value.</returns>
        public static implicit operator float(Half x)
        {
            if (x.value == 0x7c00) return float.PositiveInfinity;
            if (x.value == 0xfc00) return float.NegativeInfinity;
            if (x.value == 0x7e00) return float.NaN;
            if (x.value == 0) return 0;

            // Decode bit representations of the components of the 16-bit float.
            // Bits as stated in IEEE 754r: 1 + 5 + 10 (sign + exponent + significand)
            int sign = (x.value >> 15) & 0x1;
            int exponent = (x.value >> 10) & 0x1f;
            int significand = x.value & 0x3ff; // without the implicit bit

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

        /// <summary>
        /// Is an object equal to this object.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (!(obj is Half)) return false;
            Half x = (Half)obj;
            return value == x.value;
        }

        /// <summary>
        /// Hash function for Half.
        /// </summary>
        public override int GetHashCode()
        {
            return value;
        }

        /// <summary>
        /// Returns a string representation of the Half.
        /// </summary>
        public override string ToString()
        {
            return value.ToString();
        }
    }
}
