using System;
using AW2.Helpers;

namespace AW2.Core
{
    public class BackgroundTask
    {
        private class TaskStatus
        {
            public Exception Exception { get; set; }
            public bool IsCompleted { get; set; }
        }

        private Action _task;
        private WorkItem _taskWorkItem;

        public bool TaskRunning { get { return _taskWorkItem != null; } }
        public bool TaskCompleted { get { return _taskWorkItem == null ? false : ((TaskStatus)_taskWorkItem.State).IsCompleted; } }

        public void StartTask(Action task)
        {
            if (_taskWorkItem != null) throw new InvalidOperationException("Cannot change background task while it's running");
            _task = task;
            _taskWorkItem = AbortableThreadPool.QueueUserWorkItem(RunTask, new TaskStatus());
        }

        /// <summary>
        /// Blocks until the task finishes, ties up loose ends and then forgets all about the task.
        /// There will be no blocking if <see cref="TaskCompleted"/> is <c>true</c>.
        /// </summary>
        public void FinishTask()
        {
            if (_taskWorkItem == null)
                throw new InvalidOperationException("There is no background task to finish");
            var status = (TaskStatus)_taskWorkItem.State;

            // Block until the task completes.
            while (!status.IsCompleted) System.Threading.Thread.Sleep(0);

            if (status.Exception != null)
                throw new ApplicationException("Exception thrown during background task", status.Exception);
            ResetTask();
        }

        public void AbortTask()
        {
            if (_taskWorkItem == null) throw new InvalidOperationException("There is no background task to abort");
            AbortableThreadPool.Abort(_taskWorkItem, true);
            ResetTask();
        }

        private void ResetTask()
        {
            _taskWorkItem = null;
            _task = null;
        }

        /// <summary>
        /// Runs the task, handling exceptions. This method is to be run in a background thread.
        /// </summary>
        private void RunTask(object state)
        {
            var status = (TaskStatus)state;
            try
            {
                _task();
            }
#if !DEBUG
            catch (Exception e)
            {
                status.Exception = e;
            }
#endif
            finally
            {
                status.IsCompleted = true;
            }
        }
    }
}
