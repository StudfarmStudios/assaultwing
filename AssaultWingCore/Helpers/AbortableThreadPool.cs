using System;
using System.Collections.Generic;
using System.Threading;

namespace AW2.Helpers
{
    // AbortableThreadPool and its helpers WorkItem and WorkItemStatus are from
    // Stephen Toub's column ".NET Matters", March 2006 article "Abortable Thread Pool":
    // http://msdn.microsoft.com/en-us/magazine/cc163644.aspx
    // Slightly modified afterwards.

    /// <summary>
    /// A queued item in an abortable thread pool.
    /// </summary>
    public sealed class WorkItem
    {
        /// <summary>
        /// Creates a new work item.
        /// </summary>
        /// <param name="wc">The work itself.</param>
        /// <param name="state">State information that was passed to the work callback.</param>
        /// <param name="ctx">Context from which the work item was queued.</param>
        internal WorkItem(ContextCallback wc, object state, ExecutionContext ctx)
        {
            Callback = wc;
            State = state;
            Context = ctx;
        }

        /// <summary>
        /// The work itself.
        /// </summary>
        internal ContextCallback Callback { get; private set; }

        /// <summary>
        /// The state information passed to the work callback.
        /// </summary>
        internal object State { get; private set; }

        /// <summary>
        /// The execution context from which the work item was queued.
        /// </summary>
        internal ExecutionContext Context { get; private set; }
    }

    /// <summary>
    /// Status of a work item in an abortable thread pool.
    /// </summary>
    public enum WorkItemStatus
    {
        /// <summary>
        /// The task has completed.
        /// </summary>
        Completed, 
        
        /// <summary>
        /// The task has been queued but hasn't started execution.
        /// </summary>
        Queued,

        /// <summary>
        /// The task has started executing but hasn't finished.
        /// </summary>
        Executing,

        /// <summary>
        /// The task as been aborted.
        /// </summary>
        Aborted
    }

    /// <summary>
    /// A thread pool where task items can be aborted.
    /// </summary>
    public static class AbortableThreadPool
    {
        private static LinkedList<WorkItem> _callbacks =
          new LinkedList<WorkItem>();
        private static Dictionary<WorkItem, Thread> _threads =
          new Dictionary<WorkItem, Thread>();

        /// <summary>
        /// Queues a work item to be executed in a background thread.
        /// </summary>
        /// <param name="callback">The task to execute.</param>
        /// <param name="state">Optional state information for the task.</param>
        /// <returns>A handle for the queued work item.</returns>
        public static WorkItem QueueUserWorkItem(ContextCallback callback, object state)
        {
            if (callback == null) throw new ArgumentNullException("callback");
            WorkItem item = new WorkItem(callback, state, ExecutionContext.Capture());
            lock (_callbacks) _callbacks.AddLast(item);
            ThreadPool.QueueUserWorkItem(new WaitCallback(HandleItem));
            return item;
        }

        private static void HandleItem(object ignored)
        {
            WorkItem item = null;
            try
            {
                lock (_callbacks)
                {
                    if (_callbacks.Count > 0)
                    {
                        item = _callbacks.First.Value;
                        _callbacks.RemoveFirst();
                    }
                    if (item == null) return;
                    _threads.Add(item, Thread.CurrentThread);
                }

                ExecutionContext.Run(item.Context, item.Callback, item.State);
            }
            finally
            {
                lock (_callbacks)
                {
                    if (item != null) _threads.Remove(item);
                }
            }
        }

        /// <summary>
        /// Aborts a previously queued work item.
        /// </summary>
        /// <param name="item">The work item to abort.</param>
        /// <param name="unconditionalAbort">If <c>true</c> then abort will commence
        /// unconditionally. If <c>false</c> then the task will only be
        /// aborted if it hasn't started execution yet.</param>
        /// <returns>The status of the work item as it was before aborting.</returns>
        public static WorkItemStatus Abort(WorkItem item, bool unconditionalAbort)
        {
            if (item == null) throw new ArgumentNullException("item");

            Thread joiningThread = null;
            WorkItemStatus returnValue;
            lock (_callbacks)
            {
                LinkedListNode<WorkItem> node = _callbacks.Find(item);
                if (node != null)
                {
                    _callbacks.Remove(node);
                    returnValue = WorkItemStatus.Queued;
                }
                else if (_threads.ContainsKey(item))
                {
                    if (unconditionalAbort)
                    {
                        _threads[item].Abort();
                        joiningThread = _threads[item];
                        _threads.Remove(item);
                        returnValue = WorkItemStatus.Aborted;
                    }
                    else 
                        returnValue = WorkItemStatus.Executing;
                }
                else 
                    returnValue = WorkItemStatus.Completed;
            }
            if (joiningThread != null)
                joiningThread.Join(1000);
            return returnValue;
        }
    }
}
