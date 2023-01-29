using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

namespace AW2.UI
{
    public class CharacterSet
    {
        private class CharRangeFinder : IComparer
        {
            public int Compare(object x, object y)
            {
                if (x is char) return -Compare(y, x);
                var range = (Tuple<char, char>)x;
                var ch = (char)y;
                return
                    range.Item2 < ch ? -1 :
                    range.Item1 > ch ? 1 :
                    0;
            }
        }

        private static CharRangeFinder g_charRangeFinder = new CharRangeFinder();

        /// <summary>
        /// Character ranges, both ends inclusive.
        /// </summary>
        public Tuple<char, char>[] _charRanges;

        public CharacterSet(IEnumerable<char> chars)
        {
            _charRanges = GetRanges(chars.OrderBy(x => x).Distinct()).ToArray();
        }

        public bool Contains(char ch)
        {
            return Array.BinarySearch(_charRanges, ch, g_charRangeFinder) >= 0;
        }

        private IEnumerable<Tuple<char, char>> GetRanges(IEnumerable<char> sortedChars)
        {
            char? start = null;
            var previous = '\x0';
            foreach (var ch in sortedChars)
            {
                if (!start.HasValue) start = ch;
                else if (ch != previous + 1)
                {
                    yield return Tuple.Create(start.Value, previous);
                    start = ch;
                }
                previous = ch;
            }
            if (start.HasValue) yield return Tuple.Create(start.Value, previous);
        }
    }
}
