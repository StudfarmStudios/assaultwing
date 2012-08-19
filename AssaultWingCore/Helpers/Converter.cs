using System.Runtime.InteropServices;

namespace AW2.Helpers
{
    /// <summary>
    /// Contains static methods for converting values by reinterpreting their bit representations.
    /// </summary>
    public static class Converter
    {
        /// <summary>
        /// A helper struct to reinterpret the bit representation of an Int32 as Single and vice versa.
        /// </summary>
        /// Idea from Jon Skeet's post on 
        /// http://bytes.com/groups/net-c/274876-how-do-bitconverter-singletoint32bits
        [StructLayout(LayoutKind.Explicit)]
        private struct Int32SingleUnion
        {
            public Int32SingleUnion(float x) { _int = 0; _float = x; }
            public Int32SingleUnion(int x) { _float = 0; _int = x; }

            [FieldOffset(0)]
            public int _int;

            [FieldOffset(0)]
            public float _float;
        }

        /// <summary>
        /// A helper struct to reinterpret the bit representation of an Int16 as <see cref="Half"/> and vice versa.
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        private struct Int16HalfUnion
        {
            public Int16HalfUnion(Half x) { _short = 0; _half = x; }
            public Int16HalfUnion(short x) { _half = Half.Zero; _short = (ushort)x; }

            [FieldOffset(0)]
            public int _short; // Note: Profiling shows that int is 15 times faster here than short.

            [FieldOffset(0)]
            public Half _half;
        }

        /// <summary>
        /// Converts int to float by reinterpreting bits.
        /// </summary>
        public static float IntToFloat(int x)
        {
            return new Int32SingleUnion(x)._float;
        }

        /// <summary>
        /// Converts float to int by reinterpreting bits.
        /// </summary>
        public static int FloatToInt(float x)
        {
            return new Int32SingleUnion(x)._int;
        }

        /// <summary>
        /// Converts short to Half by reinterpreting bits.
        /// </summary>
        public static Half ShortToHalf(short x)
        {
            return new Int16HalfUnion(x)._half;
        }

        /// <summary>
        /// Converts Half to short by reinterpreting bits.
        /// </summary>
        public static short HalfToShort(Half x)
        {
            return (short)new Int16HalfUnion(x)._short;
        }
    }
}
