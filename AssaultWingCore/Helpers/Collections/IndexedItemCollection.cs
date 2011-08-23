using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AW2.Helpers.Collections
{
    /// <summary>
    /// A collection of indexed items.
    /// </summary>
    /// <typeparam name="T">The element type of the collection.</typeparam>
    public class IndexedItemCollection<T> : IList<T>, IObservableCollection<int, T>
    {
        private List<T> _items = new List<T>();

        #region IObservableCollection<int, T> Members

        public event Action<T> Added;
        public event Action<T> Removed;
        public event Func<int, T> NotFound;

        #endregion

        /// <summary>
        /// Removes items that satisfy a condition.
        /// </summary>
        /// <param name="condition">The condition by which to remove items.</param>
        public void Remove(Predicate<T> condition)
        {
            for (int index = _items.Count - 1; index >= 0; --index)
                if (condition(_items[index]))
                    RemoveAt(index);
        }

        #region IList<T> Members

        /// <summary>
        /// Determines the index of a specific item in the list.
        /// </summary>
        public int IndexOf(T item)
        {
            return _items.IndexOf(item);
        }

        /// <summary>
        /// Inserts an item to the list at the specified index.
        /// </summary>
        public void Insert(int index, T item)
        {
            _items.Insert(index, item);
            if (Added != null) Added(item);
        }

        /// <summary>
        /// Removes the list item at the specified index.
        /// </summary>
        public void RemoveAt(int index)
        {
            T removed = _items[index];
            _items.RemoveAt(index);
            if (Removed != null) Removed(removed);
        }

        /// <summary>
        /// The list item at the specified index.
        /// </summary>
        public T this[int index]
        {
            get
            {
                if (index >= 0 && index < _items.Count)
                    return _items[index];
                else if (NotFound != null)
                    return NotFound(index);
                else
                    throw new ArgumentOutOfRangeException("No value in IndexedItemCollection for index " + index);
            }
            set
            {
                _items[index] = value;
                if (Added != null) Added(value);
            }
        }

        #endregion

        #region ICollection<T> Members

        /// <summary>
        /// Adds an item to the collection.
        /// </summary>
        public void Add(T item)
        {
            _items.Add(item);
            if (Added != null) Added(item);
        }

        /// <summary>
        /// Removes all items from the collection.
        /// </summary>
        public void Clear()
        {
            var copy = _items.ToArray();
            _items.Clear();
            if (Removed != null) foreach (var item in copy) Removed(item);
        }

        /// <summary>
        /// Determines whether the collection contains a specific value.
        /// </summary>
        public bool Contains(T item)
        {
            return _items.Contains(item);
        }

        /// <summary>
        /// Copies the elements of the collection to an array, starting at a particular array index.
        /// </summary>
        public void CopyTo(T[] array, int arrayIndex)
        {
            _items.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// The number of elements contained in the collection.
        /// </summary>
        public int Count { get { return _items.Count; } }

        /// <summary>
        /// Is the collection read-only.
        /// </summary>
        public bool IsReadOnly { get { return false; } }

        /// <summary>
        /// Removes the first occurrence of a specific element from the collection.
        /// </summary>
        public bool Remove(T item)
        {
            int index = _items.IndexOf(item);
            if (index >= 0)
            {
                var removed = _items[index];
                _items.RemoveAt(index);
                if (Removed != null) Removed(removed);
                return true;
            }
            else 
                return false;
        }

        #endregion

        #region IEnumerable<T> Members

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        public IEnumerator<T> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return ((System.Collections.IEnumerable)_items).GetEnumerator();
        }

        #endregion
    }
}
