using System;

namespace AW2.Helpers.Collections
{
    /// <summary>
    /// A queue that hands out elements based on their type.
    /// </summary>
    public interface ITypedQueue<T>
    {
        /// <summary>
        /// Adds an element to the end of the queue.
        /// </summary>
        void Enqueue(T element);

        /// <summary>
        /// Removes and returns the first element in the queue that is of a type.
        /// Throws <see cref="InvalidOperationException"/> if there are no elements.
        /// </summary>
        /// <typeparam name="U">The type of element to dequeue.</typeparam>
        /// <returns>The first element in the queue of the type.</returns>
        /// <seealso cref="TryDequeue"/>
        U Dequeue<U>() where U : T;

        /// <summary>
        /// Removes and returns the first element in the queue that is of a type.
        /// If the element doesn't match the given criteria or there are no elements of
        /// the type, default(U) is returned.
        /// </summary>
        U TryDequeue<U>(Predicate<U> criteria) where U : T;
        U TryDequeue<U>() where U : T;

        /// <summary>
        /// Adds an element to the front of the queue.
        /// </summary>
        /// Use this method to undo a previous call to <see cref="Dequeue&lt;U&gt;()"/>,
        /// putting an element back to where it was. This method may have far
        /// worse performance than <see cref="Enqueue(T)"/>.
        /// <param name="element">The element to add.</param>
        void Requeue(T element);

        /// <summary>
        /// Returns the number of elements of a type in the queue.
        /// </summary>
        /// <typeparam name="U">The type of elements to count.</typeparam>
        /// <returns>The number of such elements in the queue.</returns>
        int Count<U>() where U : T;

        /// <summary>
        /// Prunes old elements off the queue.
        /// Elements older than <paramref name="timeout"/> are removed from the queue
        /// and passed to <paramref name="action"/> if it is not <c>null</c>.
        /// </summary>
        /// <param name="action">Action to perform on purged elements, or <c>null</c>.</param>
        /// <param name="timeout">Elements older than this are purged.</param>
        void Prune(TimeSpan timeout, Action<T> action);
    }
}
