using System;

namespace AW2.Helpers.Collections
{
    /// <summary>
    /// A collection that raises events when it is modified.
    /// </summary>
    /// <typeparam name="T">The element type of the collection.</typeparam>
    public interface IObservableCollection<T>
    {
        /// <summary>
        /// Called when an item has been removed from the collection.
        /// The argument is the removed item.
        /// </summary>
        event Action<T> Removed;

        /// <summary>
        /// Called when an item was not found from the collection,
        /// in place of throwing an exception.
        /// The argument describes which item was looked for.
        /// The expected return value is a substitute item.
        /// </summary>
        event Func<object, T> NotFound;
    }
}
