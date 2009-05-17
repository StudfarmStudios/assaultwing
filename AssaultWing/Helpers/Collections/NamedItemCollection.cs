using System;
using System.Collections.Generic;
using System.Linq;

namespace AW2.Helpers.Collections
{
    /// <summary>
    /// A collection of named items.
    /// </summary>
    /// <typeparam name="T">The element type of the collection.</typeparam>
    public class NamedItemCollection<T> : IDictionary<string, T>, IObservableCollection<T>
    {
        Dictionary<string, T> items = new Dictionary<string, T>();

        #region IObservableCollection<T> Members

        /// <summary>
        /// Called when an item has been removed from the collection.
        /// The argument is the removed item.
        /// </summary>
        public event Action<T> Removed;

        /// <summary>
        /// Called when an item was not found from the collection,
        /// in place of throwing an exception.
        /// The argument describes which item was looked for.
        /// The expected return value is a substitute item.
        /// </summary>
        public event Func<object, T> NotFound;

        #endregion

        #region IDictionary<string, T> Members

        /// <summary>
        /// Adds an element with the provided key and value to the dictionary.
        /// </summary>
        public void Add(string key, T value)
        {
            items[key] = value;
        }

        /// <summary>
        /// Determines whether the dictionary contains an element with the specified key.
        /// </summary>
        public bool ContainsKey(string key)
        {
            return items.ContainsKey(key);
        }

        /// <summary>
        /// The keys of the dictionary.
        /// </summary>
        public ICollection<string> Keys { get { return items.Keys; } }

        /// <summary>
        /// Removes the element with the specified key from the dictionary.
        /// </summary>
        /// <returns><c>true</c> if the element is successfully removed.
        /// <c>false</c> if key was not found or could not be removed.</returns>
        public bool Remove(string key)
        {
            T value;
            if (items.TryGetValue(key, out value))
            {
                bool result = items.Remove(key);
                if (Removed != null) Removed(value);
                return result;
            }
            return false;
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        public bool TryGetValue(string key, out T value)
        {
            return items.TryGetValue(key, out value);
        }

        /// <summary>
        /// The values in the dictionary.
        /// </summary>
        public ICollection<T> Values { get { return items.Values; } }

        /// <summary>
        /// The element with the specified key.
        /// </summary>
        public T this[string key]
        {
            get
            {
                T value;
                if (items.TryGetValue(key, out value))
                    return value;
                else if (NotFound != null)
                    return NotFound(key);
                else
                    throw new KeyNotFoundException("NamedItemCollection has no value for key " + key);
            }
            set { items[key] = value; }
        }

        #endregion

        #region ICollection<KeyValuePair<string, T>> Members

        /// <summary>
        /// Adds an item to the collection.
        /// </summary>
        void ICollection<KeyValuePair<string, T>>.Add(KeyValuePair<string, T> item)
        {
            ((ICollection<KeyValuePair<string, T>>)items).Add(item);
        }

        /// <summary>
        /// Removes all items from the collection.
        /// </summary>
        public void Clear()
        {
            if (Removed != null)
            {
                T[] removedItems = items.Values.ToArray();
                items.Clear();
                foreach (T item in removedItems) Removed(item);
            }
            else
                items.Clear();
        }

        /// <summary>
        /// Determines whether the collection contains a specific value.
        /// </summary>
        bool ICollection<KeyValuePair<string, T>>.Contains(KeyValuePair<string, T> item)
        {
            return ((ICollection<KeyValuePair<string, T>>)items).Contains(item);
        }

        /// <summary>
        /// Copies the elements of the collection to an array, starting at a particular array index.
        /// </summary>
        void ICollection<KeyValuePair<string, T>>.CopyTo(KeyValuePair<string, T>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<string, T>>)items).CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// The number of elements contained in the collection.
        /// </summary>
        public int Count { get { return items.Count; } }

        /// <summary>
        /// Is the collection read-only.
        /// </summary>
        public bool IsReadOnly { get { return false; } }

        /// <summary>
        /// Removes the first occurrence of a specific element from the collection.
        /// </summary>
        bool ICollection<KeyValuePair<string, T>>.Remove(KeyValuePair<string, T> item)
        {
            if (items.Contains(item))
            {
                bool result = ((ICollection<KeyValuePair<string, T>>)items).Remove(item);
                if (Removed != null) Removed(item.Value);
                return result;
            }
            return false;
        }

        #endregion

        #region IEnumerable<KeyValuePair<string, T>> Members

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        public IEnumerator<KeyValuePair<string, T>> GetEnumerator()
        {
            return items.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return ((System.Collections.IEnumerable)items).GetEnumerator();
        }

        #endregion
    }
}
