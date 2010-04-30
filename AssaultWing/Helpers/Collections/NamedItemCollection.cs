using System;
using System.Collections.Generic;
using System.Linq;

namespace AW2.Helpers.Collections
{
    /// <summary>
    /// A collection of named items.
    /// </summary>
    /// <typeparam name="T">The element type of the collection.</typeparam>
    public class NamedItemCollection<T> : IDictionary<CanonicalString, T>, IObservableCollection<CanonicalString, T>
        where T : class
    {
        private List<T> _items = new List<T>();

        #region IObservableCollection<CanonicalString, T> Members

        public event Action<T> Added;
        public event Action<T> Removed;
        public event Func<CanonicalString, T> NotFound;

        #endregion

        #region IDictionary<CanonicalString, T> Members

        /// <summary>
        /// Adds an element with the provided key and value to the dictionary.
        /// Throws an exception if an element with the same key already exists.
        /// </summary>
        public void Add(CanonicalString key, T value)
        {
            if (value == null) throw new InvalidOperationException("Cannot add null value to a NamedItemCollection");
            while (_items.Count <= key.Canonical) _items.Add(null);
            if (_items[key.Canonical] != null) throw new ArgumentException("An element with key " + key + " already exists");
            _items[key.Canonical] = value;
            if (Added != null) Added(value);
        }

        /// <summary>
        /// Determines whether the dictionary contains an element with the specified key.
        /// </summary>
        public bool ContainsKey(CanonicalString key)
        {
            return _items.Count > key.Canonical && _items[key.Canonical] != null;
        }

        /// <summary>
        /// The keys of the dictionary.
        /// </summary>
        public ICollection<CanonicalString> Keys
        {
            get
            {
                var keys = new List<CanonicalString>();
                for (int i = 0; i < _items.Count; ++i)
                    if (_items[i] != null) keys.Add(new CanonicalString(i));
                return keys;
            }
        }

        /// <summary>
        /// Removes the element with the specified key from the dictionary.
        /// </summary>
        /// <returns><c>true</c> if the element was successfully removed.
        /// <c>false</c> if key was not found or could not be removed.</returns>
        public bool Remove(CanonicalString key)
        {
            if (key.Canonical < 0 || key.Canonical >= _items.Count) return false;
            T value = _items[key.Canonical];
            if (value == null) return false;
            _items.RemoveAt(key.Canonical);
            if (Removed != null) Removed(value);
            return true;
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <returns><c>true</c> if the dictionary contains an element with the key,
        /// otherwise <c>false</c>.</returns>
        public bool TryGetValue(CanonicalString key, out T value)
        {
            if (key.Canonical < 0 || key.Canonical >= _items.Count)
            {
                value = null;
                return false;
            }
            value = _items[key.Canonical];
            return value != null;
        }

        /// <summary>
        /// The values in the dictionary.
        /// </summary>
        public ICollection<T> Values { get { return _items.FindAll(item => item != null); } }

        /// <summary>
        /// The element with the specified key.
        /// </summary>
        public T this[CanonicalString key]
        {
            get
            {
                T value;
                if (TryGetValue(key, out value))
                    return value;
                else if (NotFound != null)
                    return NotFound(key);
                else
                    throw new KeyNotFoundException("NamedItemCollection has no value for key " + key);
            }
            set
            {
                if (value == null) throw new InvalidOperationException("Cannot add null value to a NamedItemCollection");
                while (_items.Count <= key.Canonical) _items.Add(null);
                _items[key.Canonical] = value;
                if (Added != null) Added(value);
            }
        }

        #endregion

        #region ICollection<KeyValuePair<CanonicalString, T>> Members

        /// <summary>
        /// Adds an item to the collection.
        /// </summary>
        void ICollection<KeyValuePair<CanonicalString, T>>.Add(KeyValuePair<CanonicalString, T> item)
        {
            Add(item.Key, item.Value);
            if (Added != null) Added(item.Value);
        }

        /// <summary>
        /// Removes all items from the collection.
        /// </summary>
        public void Clear()
        {
            _items.Clear();
        }

        /// <summary>
        /// Determines whether the collection contains a specific value.
        /// </summary>
        bool ICollection<KeyValuePair<CanonicalString, T>>.Contains(KeyValuePair<CanonicalString, T> item)
        {
            return ContainsKey(item.Key) && _items[item.Key.Canonical] == item.Value;
        }

        /// <summary>
        /// Copies the elements of the collection to an array, starting at a particular array index.
        /// </summary>
        void ICollection<KeyValuePair<CanonicalString, T>>.CopyTo(KeyValuePair<CanonicalString, T>[] array, int arrayIndex)
        {
            if (array == null) throw new ArgumentNullException("Cannot copy to a null array");
            if (arrayIndex < 0) throw new ArgumentOutOfRangeException("Negative array index");
            if (array.Rank != 1) throw new ArgumentException("Cannot copy to a multidimensional array");
            if (arrayIndex + Count > array.Length) throw new ArgumentException("Not enough space on array");
            int writeI = arrayIndex;
            for (int i = 0; i < _items.Count; ++i)
                if (_items[i] != null)
                    array[writeI++] = new KeyValuePair<CanonicalString, T>(new CanonicalString(i), _items[i]);
        }

        /// <summary>
        /// The number of elements contained in the collection.
        /// </summary>
        public int Count { get { return _items.Count(item => item != null); } }

        /// <summary>
        /// Is the collection read-only.
        /// </summary>
        public bool IsReadOnly { get { return false; } }

        /// <summary>
        /// Removes the first occurrence of a specific element from the collection.
        /// </summary>
        /// <returns><c>true</c> if the element was successfully removed.
        /// <c>false</c> if key was not found or could not be removed.</returns>
        bool ICollection<KeyValuePair<CanonicalString, T>>.Remove(KeyValuePair<CanonicalString, T> item)
        {
            if (!((ICollection<KeyValuePair<CanonicalString, T>>)this).Contains(item)) return false;
            return Remove(item.Key);
        }

        #endregion

        #region IEnumerable<KeyValuePair<CanonicalString, T>> Members

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        public IEnumerator<KeyValuePair<CanonicalString, T>> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IEnumerable Members

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
