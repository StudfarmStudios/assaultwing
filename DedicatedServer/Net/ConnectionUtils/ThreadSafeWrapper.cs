using System;

namespace AW2.Net.ConnectionUtils
{
    /// <summary>
    /// Wraps an object, making sure the programmer won't forget thread safety.
    /// Thread-safety cannot be guaranteed if the reference to the wrapped object 
    /// is deliberately leaked out.
    /// </summary>
    /// <typeparam name="T">Type of the wrapped object.</typeparam>
    public class ThreadSafeWrapper<T>
    {
        T obj;

        /// <summary>
        /// Performs an action on the wrapped object.
        /// </summary>
        /// <param name="action">The action to perform.</param>
        public void Do(Action<T> action)
        {
            lock (obj) action(obj);
        }

        /// <summary>
        /// Creates a thread safe wrapper around an object.
        /// </summary>
        /// <param name="obj">The object to wrap.</param>
        public ThreadSafeWrapper(T obj)
        {
            if (obj == null) throw new ArgumentNullException("Cannot wrap a null reference");
            this.obj = obj;
        }
    }
}
