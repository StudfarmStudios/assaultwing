using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AW2.Helpers;

namespace AW2.Helpers.Collections
{
    /// <summary>
    /// A queue that hands out elements based on their type.
    /// </summary>
    /// Thread-safe.
    /// <typeparam name="T">Base type of the elements in the queue.</typeparam>
    public class TypedQueue<T> : ITypedQueue<T>
    {
        /// <summary>
        /// Mapping of element type to a queue holding 
        /// the elements with their enqueuing timestamps.
        /// </summary>
        Dictionary<Type, Queue<Pair<T, TimeSpan>>> queues;

        /// <summary>
        /// Creates an empty typed queue.
        /// </summary>
        public TypedQueue()
        {
            queues = new Dictionary<Type, Queue<Pair<T, TimeSpan>>>();
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
                Queue<Pair<T, TimeSpan>> subqueue;
                if (!queues.TryGetValue(elementType, out subqueue))
                    subqueue = queues[elementType] = new Queue<Pair<T, TimeSpan>>();
                TimeSpan now = AssaultWingCore.Instance.GameTime.TotalRealTime;
                subqueue.Enqueue(new Pair<T, TimeSpan>(element, now));
            }
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
            Type elementType = typeof(U);
            lock (queues)
            {
                Queue<Pair<T, TimeSpan>> subqueue;
                if (!queues.TryGetValue(elementType, out subqueue) || subqueue.Count == 0)
                    throw new InvalidOperationException("Cannot dequeue empty queue");
                return (U)subqueue.Dequeue().First;
            }
        }

        public U TryDequeue<U>(Predicate<U> criteria) where U : T
        {
            var elementType = typeof(U);
            lock (queues)
            {
                Queue<Pair<T, TimeSpan>> subqueue;
                if (!queues.TryGetValue(elementType, out subqueue) || subqueue.Count == 0)
                    return default(U);
                if (!criteria((U)subqueue.Peek().First)) return default(U);
                return (U)subqueue.Dequeue().First;
            }
        }

        public U TryDequeue<U>() where U : T
        {
            return TryDequeue<U>(x => true);
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
            Type elementType = element.GetType();
            lock (queues)
            {
                // NOTE: The element's timestamp is refreshed.
                TimeSpan now = AssaultWingCore.Instance.GameTime.TotalRealTime;
                Queue<Pair<T, TimeSpan>> oldQueue;
                var newQueue = new Queue<Pair<T, TimeSpan>>();
                newQueue.Enqueue(new Pair<T, TimeSpan>(element, now));
                if (queues.TryGetValue(elementType, out oldQueue))
                    foreach (var pair in oldQueue) newQueue.Enqueue(pair);
                queues[elementType] = newQueue;
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
                Queue<Pair<T, TimeSpan>> queue;
                if (queues.TryGetValue(elementType, out queue))
                    return queue.Count;
                return 0;
            }
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
            TimeSpan now = AssaultWingCore.Instance.GameTime.TotalRealTime;
            lock (queues)
            {
                foreach (var type in queues.Keys.ToArray())
                {
                    var queue = queues[type];
                    if (queue.Any(pair => now - pair.Second > timeout))
                    {
                        // The queue contains old elements.
                        // Dequeue the other elements to form a new queue.
                        var newQueue = new Queue<Pair<T, TimeSpan>>();
                        while (queue.Any())
                        {
                            var pair = queue.Dequeue();
                            if (now - pair.Second > timeout)
                            {
                                if (action != null)
                                    action(pair.First);
                            }
                            else
                                newQueue.Enqueue(pair);
                        }
                        queues[type] = newQueue;
                    }
                }
            }
        }
    }
}
