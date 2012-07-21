using System;
using System.Collections.Concurrent;
using System.Threading;

namespace AW2.Helpers.Collections
{
    /// <summary>
    /// A queue of work items that is being processed in a worker thread.
    /// </summary>
    public class WorkQueue<T> : IDisposable
    {
        private BlockingCollection<T> _queue;
        private Action<T> _processItem;
        private bool _disposed;

        private Action _workingFinished;
        public int Count { get { return _queue.Count; } }

        public WorkQueue(Action<T> processItem, Action workingFinished)
        {
            _queue = new BlockingCollection<T>();
            _processItem = processItem;
            _workingFinished = workingFinished;
            StartWorking();
        }

        public void Enqueue(T item)
        {
            _queue.Add(item);
        }

        public void NoMoreWork()
        {
            _queue.CompleteAdding();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _queue.CompleteAdding();
        }

        /// <summary>
        /// Starts a worker thread that processes enqueued work items as they appear.
        /// </summary>
        private void StartWorking()
        {
            var keepWorking = (Action)KeepWorking;
            keepWorking.BeginInvoke(OnWorkingFinished, keepWorking);
        }

        private void KeepWorking()
        {
            while (true)
            {
                T item = default(T);
                try
                {
                    item = _queue.Take();
                }
                catch (InvalidOperationException)
                {
                    // _queue is empty and the collection has been marked as complete for adding before Take().
                    break;
                }
                _processItem(item);
            }
            _queue.Dispose();
        }

        private void OnWorkingFinished(IAsyncResult asyncResult)
        {
            Log.Write("WorkQueue finished");
            if (_workingFinished != null) _workingFinished();
            ((Action)asyncResult.AsyncState).EndInvoke(asyncResult);
        }
    }
}
