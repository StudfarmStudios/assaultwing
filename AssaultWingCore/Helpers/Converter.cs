using System.Runtime.InteropServices;

namespace AW2.Helpers
{
    /// <summary>
    /// Contains static methods for converting values by
    /// reinterpreting their bit representations.
    /// </summary>
    public static class Converter
    {
        /// <summary>
        /// A way to switch the interpretation of 32 bits between int and float.
        /// </summary>
        /// Idea from Jon Skeet's post on 
        /// http://bytes.com/groups/net-c/274876-how-do-bitconverter-singletoint32bits
        [StructLayout(LayoutKind.Explicit)]
        struct Int32SingleUnion
        {
            /// <summary>
            /// Initialises bits with a float value.
            /// </summary>
            public Int32SingleUnion(float x) { i = 0; f = x; }

            /// <summary>
            /// Initialises bits with an int value.
            /// </summary>
            /// <param name="x"></param>
            public Int32SingleUnion(int x) { f = 0; i = x; }

            /// <summary>
            /// Int32 version of the value.
            /// </summary>
            [FieldOffset(0)]
            public int i;

            /// <summary>
            /// Single version of the value.
            /// </summary>
            [FieldOffset(0)]
            public float f;
        }

        [StructLayout(LayoutKind.Explicit)]
        struct Int16HalfUnion
        {
            /// <summary>
            /// Initialises bits with a Half value.
            /// </summary>
            public Int16HalfUnion(Half x) { i = 0; f = x; }

            /// <summary>
            /// Initialises bits with an int value.
            /// </summary>
            public Int16HalfUnion(short x) { f = new Half(0); i = x; }

            /// <summary>
            /// Int32 version of the value.
            /// </summary>
            [FieldOffset(0)]
            public short i;

            /// <summary>
            /// Half version of the value.
            /// </summary>
            [FieldOffset(0)]
            public Half f;
        }

        /// <summary>
        /// Converts int to float by reinterpreting bits.
        /// </summary>
        public static float IntToFloat(int x)
        {
            var converter = new Int32SingleUnion(x);
            return converter.f;
        }

        /// <summary>
        /// Converts float to int by reinterpreting bits.
        /// </summary>
        public static int FloatToInt(float x)
        {
            var converter = new Int32SingleUnion(x);
            return converter.i;
        }

        /// <summary>
        /// Converts short to Half by reinterpreting bits.
        /// </summary>
        public static Half ShortToHalf(short x)
        {
            var converter = new Int16HalfUnion(x);
            return converter.f;
        }

        /// <summary>
        /// Converts Half to short by reinterpreting bits.
        /// </summary>
        public static short HalfToShort(Half x)
        {
            var converter = new Int16HalfUnion(x);
            return converter.i;
        }
    }
}
