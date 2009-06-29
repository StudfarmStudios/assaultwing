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
        List<T> items = new List<T>();

        #region IObservableCollection<int, T> Members

        /// <summary>
        /// Called when an item has been added to the collection.
        /// The argument is the added item.
        /// </summary>
        public event Action<T> Added;

        /// <summary>
        /// Called when an item has been removed from the collection.
        /// The argument is the removed item. Not called when the whole collection is cleared.
        /// </summary>
        public event Action<T> Removed;

        /// <summary>
        /// Called when an item was not found from the collection,
        /// in place of throwing an exception.
        /// The argument describes which item was looked for.
        /// The expected return value is a substitute item.
        /// </summary>
        public event Func<int, T> NotFound;

        #endregion

        /// <summary>
        /// Removes items that satisfy a condition.
        /// </summary>
        /// <param name="condition">The condition by which to remove items.</param>
        public void Remove(Predicate<T> condition)
        {
            for (int index = items.Count - 1; index >= 0; --index)
                if (condition(items[index]))
                    RemoveAt(index);
        }

        #region IList<T> Members

        /// <summary>
        /// Determines the index of a specific item in the list.
        /// </summary>
        public int IndexOf(T item)
        {
            return items.IndexOf(item);
        }

        /// <summary>
        /// Inserts an item to the list at the specified index.
        /// </summary>
        public void Insert(int index, T item)
        {
            items.Insert(index, item);
            if (Added != null) Added(item);
        }

        /// <summary>
        /// Removes the list item at the specified index.
        /// </summary>
        public void RemoveAt(int index)
        {
            T removed = items[index];
            items.RemoveAt(index);
            if (Removed != null) Removed(removed);
        }

        /// <summary>
        /// The list item at the specified index.
        /// </summary>
        public T this[int index]
        {
            get
            {
                if (index >= 0 && index < items.Count)
                    return items[index];
                else if (NotFound != null)
                    return NotFound(index);
                else
                    throw new ArgumentOutOfRangeException("No value in IndexedItemCollection for index " + index);
            }
            set
            {
                items[index] = value;
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
            items.Add(item);
            if (Added != null) Added(item);
        }

        /// <summary>
        /// Removes all items from the collection.
        /// </summary>
        public void Clear()
        {
            items.Clear();
        }

        /// <summary>
        /// Determines whether the collection contains a specific value.
        /// </summary>
        public bool Contains(T item)
        {
            return items.Contains(item);
        }

        /// <summary>
        /// Copies the elements of the collection to an array, starting at a particular array index.
        /// </summary>
        public void CopyTo(T[] array, int arrayIndex)
        {
            items.CopyTo(array, arrayIndex);
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
        public bool Remove(T item)
        {
            int index = items.IndexOf(item);
            if (index >= 0)
            {
                var removed = items[index];
                items.RemoveAt(index);
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
