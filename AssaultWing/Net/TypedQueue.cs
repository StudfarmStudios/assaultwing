using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace AW2.Net
{
    /// <summary>
    /// A queue that hands out elements based on their type.
    /// </summary>
    /// Thread-safe.
    public class TypedQueue : TypedQueue<object>
    {
    }

    /// <summary>
    /// A queue that hands out elements based on their type.
    /// </summary>
    /// Thread-safe.
    /// <typeparam name="T">Base type of the elements in the queue.</typeparam>
    public class TypedQueue<T>
    {
        Dictionary<Type, Queue<T>> queues;

        /// <summary>
        /// Creates an empty typed queue.
        /// </summary>
        public TypedQueue()
        {
            queues = new Dictionary<Type, Queue<T>>();
        }

        /// <summary>
        /// Adds an element to the end of the queue.
        /// </summary>
        /// <param name="element">The element to add.</param>
        public void Enqueue(T element)
        {
            Type elementType = element.GetType();
            lock (queues)
            {
                Queue<T> subqueue;
                if (!queues.TryGetValue(elementType, out subqueue))
                    subqueue = queues[elementType] = new Queue<T>();
                subqueue.Enqueue(element);
            }
            Prune();
        }

        /// <summary>
        /// Removes and returns the first element in the queue that is of a type.
        /// </summary>
        /// <typeparam name="U">The type of element to dequeue.</typeparam>
        /// <returns>The first element in the queue of the type.</returns>
        public U Dequeue<U>() where U : T
        {
            Type elementType = typeof(U);
            lock (queues)
            {
                Queue<T> subqueue;
                if (!queues.TryGetValue(elementType, out subqueue) || subqueue.Count == 0)
                    throw new InvalidOperationException("Cannot dequeue empty queue");
                return (U)subqueue.Dequeue();
            }
        }

        /// <summary>
        /// Returns the number of elements of a type in the queue.
        /// </summary>
        /// <typeparam name="U">The type of elements to count.</typeparam>
        /// <returns>The number of such elements in the queue.</returns>
        public int Count<U>() where U : T
        {
            Type elementType = typeof(U);
            lock (queues)
            {
                Queue<T> queue;
                if (queues.TryGetValue(elementType, out queue))
                    return queue.Count;
                return 0;
            }
        }

        /// <summary>
        /// Prunes old elements off the queue.
        /// </summary>
        void Prune()
        {
            // TODO: pruning of old elements.
            // - add timestamps to queue elements
            // - dequeue old elements from beginnings of each subqueue
            // - consider not pruning unless some time has passed
            // - consider announcing removal of elements to client program (event?)
        }
    }
}
