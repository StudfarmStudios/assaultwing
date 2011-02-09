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
        private Dictionary<Type, Queue<T>> queues;

        public TypedQueue()
        {
            queues = new Dictionary<Type, Queue<T>>();
        }

        public void Enqueue(T element)
        {
            var elementType = element.GetType();
            lock (queues)
            {
                Queue<T> subqueue;
                if (!queues.TryGetValue(elementType, out subqueue))
                    subqueue = queues[elementType] = new Queue<T>();
                subqueue.Enqueue(element);
            }
        }

        public U Dequeue<U>() where U : T
        {
            var elementType = typeof(U);
            lock (queues)
            {
                Queue<T> subqueue;
                if (!queues.TryGetValue(elementType, out subqueue) || subqueue.Count == 0)
                    throw new InvalidOperationException("Cannot dequeue empty queue");
                return (U)subqueue.Dequeue();
            }
        }

        public U TryDequeue<U>(Predicate<U> criteria) where U : T
        {
            var elementType = typeof(U);
            lock (queues)
            {
                Queue<T> subqueue;
                if (!queues.TryGetValue(elementType, out subqueue) || subqueue.Count == 0)
                    return default(U);
                if (!criteria((U)subqueue.Peek())) return default(U);
                return (U)subqueue.Dequeue();
            }
        }

        public U TryDequeue<U>() where U : T
        {
            return TryDequeue<U>(x => true);
        }

        public void Requeue(T element)
        {
            var elementType = element.GetType();
            lock (queues)
            {
                Queue<T> oldQueue;
                var newQueue = new Queue<T>();
                newQueue.Enqueue(element);
                if (queues.TryGetValue(elementType, out oldQueue))
                    foreach (var pair in oldQueue) newQueue.Enqueue(pair);
                queues[elementType] = newQueue;
            }
        }

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

        public void Prune(Predicate<T> condition, Action<T> action)
        {
            lock (queues)
            {
                foreach (var type in queues.Keys.ToArray())
                {
                    var queue = queues[type];
                    if (queue.Any(element => condition(element)))
                    {
                        // The queue contains old elements.
                        // Dequeue the other elements to form a new queue.
                        var newQueue = new Queue<T>();
                        while (queue.Any())
                        {
                            var element = queue.Dequeue();
                            if (condition(element))
                            {
                                if (action != null)
                                    action(element);
                            }
                            else
                                newQueue.Enqueue(element);
                        }
                        queues[type] = newQueue;
                    }
                }
            }
        }
    }
}
