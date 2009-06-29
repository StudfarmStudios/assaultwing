using System;

namespace AW2.Helpers.Collections
{
    /// <summary>
    /// A collection that raises events when it is modified.
    /// </summary>
    /// <typeparam name="TIndex">The index type of the collection.</typeparam>
    /// <typeparam name="TElement">The element type of the collection.</typeparam>
    public interface IObservableCollection<TIndex, TElement>
    {
        /// <summary>
        /// Called when an item has been added to the collection.
        /// The argument is the added item.
        /// </summary>
        event Action<TElement> Added;

        /// <summary>
        /// Called when an item has been removed from the collection.
        /// The argument is the removed item. Not called when the whole collection is cleared.
        /// </summary>
        event Action<TElement> Removed;

        /// <summary>
        /// Called when an item was not found from the collection,
        /// in place of throwing an exception.
        /// The argument describes which item was looked for.
        /// The expected return value is a substitute item.
        /// </summary>
        event Func<TIndex, TElement> NotFound;
    }
}
