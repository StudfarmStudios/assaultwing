using System;
using System.Collections.Generic;
using System.Linq;

namespace AW2.UI
{
    public class CharacterSet
    {
        /// <summary>
        /// Character ranges, both ends inclusive.
        /// </summary>
        private Tuple<char, char>[] _charRanges;

        public CharacterSet(IEnumerable<char> chars)
        {
            _charRanges = GetRanges(chars.OrderBy(x => x).Distinct()).ToArray();
        }

        public bool Contains(char ch)
        {
            return _charRanges.Any(range => range.Item1 <= ch && ch <= range.Item2);
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
