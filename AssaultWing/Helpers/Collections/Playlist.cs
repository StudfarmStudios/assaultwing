using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace AW2.Helpers.Collections
{
    /// <summary>
    /// Playlist of named items with an enumerator over the items.
    /// </summary>
    public class Playlist : ReadOnlyCollection<string>
    {
        int index;

        /// <summary>
        /// The current item.
        /// </summary>
        public string Current
        {
            get
            {
                if (index < 0 || index >= Count) throw new InvalidOperationException("Playlist enumerator not valid for Current");
                return this[index];
            }
        }

        /// <summary>
        /// The next item.
        /// </summary>
        public string Next
        {
            get
            {
                if (index >= Count - 1) throw new InvalidOperationException("No next item in playlist");
                return this[index + 1];
            }
        }

        /// <summary>
        /// Is there a next item.
        /// </summary>
        public bool HasNext { get { return index < Count - 1; } }

        /// <summary>
        /// Creates a playlist from given content.
        /// The enumerator will point to before the first item.
        /// </summary>
        public Playlist(IList<string> list)
            : base(list)
        {
            if (Count == 0) throw new ArgumentException("Empty playlist");
            Reset();
        }

        /// <summary>
        /// Resets the enumerator to point to before the first item.
        /// </summary>
        public void Reset() { index = -1; }

        /// <summary>
        /// Advances the enumerator to the next item.
        /// </summary>
        /// <returns><c>true</c> if the operation succeeded,
        /// <c>false</c> if there was no next item</returns>
        public bool MoveNext()
        {
            bool success = HasNext;
            if (success) ++index;
            return success;
        }
    }
}
