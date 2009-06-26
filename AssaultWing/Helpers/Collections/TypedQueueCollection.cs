using System;
using System.Collections.Generic;
using System.Linq;

namespace AW2.Helpers.Collections
{
    /// <summary>
    /// Combines several <see cref="ITypedQueue&lt;T&gt;"/> instances for
    /// dequeuing elements from them.
    /// </summary>
    public class TypedQueueCollection<T> : ITypedQueue<T>
    {
        IList<ITypedQueue<T>> queues = new List<ITypedQueue<T>>();
        List<T> requeuedElements = new List<T>();

        /// <summary>
        /// Adds an ITypedQueue to the collection.
        /// </summary>
        public void Add(ITypedQueue<T> queue)
        {
            queues.Add(queue);
        }

        /// <summary>
        /// Removes an ITypedQueue from the collection.
        /// </summary>
        public void Remove(ITypedQueue<T> queue)
        {
            queues.Remove(queue);
        }

        /// <summary>
        /// Adds an element to the end of the queue.
        /// </summary>
        public void Enqueue(T element)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Removes and returns the first element in the queue that is of a type.
        /// Throws <see cref="InvalidOperationException"/> if there are no elements.
        /// </summary>
        /// <typeparam name="U">The type of element to dequeue.</typeparam>
        /// <returns>The first element in the queue of the type.</returns>
        /// <seealso cref="TryDequeue"/>
        public U Dequeue<U>() where U : T
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Removes and returns the first element in the queue that is of a type.
        /// Returns the default value of <typeparamref name="U"/> if there are no such elements.
        /// </summary>
        /// <typeparam name="U">The type of element to dequeue.</typeparam>
        /// <returns>The first element in the queue of the type, or the
        /// default value of <typeparamref name="U"/> if there are no such elements.</returns>
        /// <seealso cref="Dequeue"/>
        public U TryDequeue<U>() where U : T
        {
            int index = requeuedElements.FindLastIndex(elem => elem is U);
            if (index >= 0)
            {
                U element = (U)requeuedElements[index];
                requeuedElements.RemoveAt(index);
                return element;
            }
            foreach (var queue in queues)
            {
                var result = queue.TryDequeue<U>();
                if (result != null) return result;
            }
            return default(U);
        }

        /// <summary>
        /// Adds an element to the front of the queue.
        /// </summary>
        /// Use this method to undo a previous call to <see cref="Dequeue&lt;U&gt;()"/>,
        /// putting an element back to where it was. This method may have far
        /// worse performance than <see cref="Enqueue(T)"/>.
        /// <param name="element">The element to add.</param>
        public void Requeue(T element)
        {
            requeuedElements.Add(element);
        }

        /// <summary>
        /// Returns the number of elements of a type in the queue.
        /// </summary>
        /// <typeparam name="U">The type of elements to count.</typeparam>
        /// <returns>The number of such elements in the queue.</returns>
        public int Count<U>() where U : T
        {
            return queues.Sum(queue => queue.Count<U>());
        }

        /// <summary>
        /// Prunes old elements off the queue.
        /// Elements older than <paramref name="timeout"/> are removed from the queue
        /// and passed to <paramref name="action"/> if it is not <c>null</c>.
        /// </summary>
        /// <param name="action">Action to perform on purged elements, or <c>null</c>.</param>
        /// <param name="timeout">Elements older than this are purged.</param>
        public void Prune(TimeSpan timeout, Action<T> action)
        {
            foreach (var queue in queues) queue.Prune(timeout, action);
        }
    }
}
