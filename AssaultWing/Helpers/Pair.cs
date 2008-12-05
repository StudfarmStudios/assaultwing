using System;

namespace AW2.Helpers
{
    /// <summary>
    /// A pair of values.
    /// </summary>
    /// <typeparam name="T">Type of the first element of the pair.</typeparam>
    /// <typeparam name="S">Type of the second element of the pair.</typeparam>
    public class Pair<T, S>
    {
        /// <summary>
        /// Creates a pair.
        /// </summary>
        /// <param name="first">The first element of the pair.</param>
        /// <param name="second">The second element of the pair.</param>
        public Pair(T first, S second)
        {
            First = first;
            Second = second;
        }

        /// <summary>
        /// The first element of the pair.
        /// </summary>
        public T First { get; set; }

        /// <summary>
        /// The second element of the pair.
        /// </summary>
        public S Second { get; set; }
    }
}
