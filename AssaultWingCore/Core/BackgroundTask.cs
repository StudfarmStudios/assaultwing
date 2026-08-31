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
#if BROWSER
        private bool _browserTaskRunning;
        private bool _browserTaskCompleted;
        private Exception _browserTaskException;
#else
        private WorkItem _taskWorkItem;
#endif

#if BROWSER
        public bool TaskRunning { get { return _browserTaskRunning; } }
        public bool TaskCompleted { get { return _browserTaskRunning && _browserTaskCompleted; } }
#else
        public bool TaskRunning { get { return _taskWorkItem != null; } }
        public bool TaskCompleted { get { return _taskWorkItem == null ? false : ((TaskStatus)_taskWorkItem.State).IsCompleted; } }
#endif

        public void StartTask(Action task)
        {
#if BROWSER
            if (_browserTaskRunning) throw new InvalidOperationException("Cannot change background task while it's running");
            _task = task;
            _browserTaskRunning = true;
            _browserTaskCompleted = false;
            _browserTaskException = null;
            try
            {
                // Browser builds are single-threaded. Run the arena preparation
                // synchronously and report completion on the following game tick.
                _task();
            }
            catch (Exception e)
            {
                _browserTaskException = e;
            }
            finally
            {
                _browserTaskCompleted = true;
            }
#else
            if (_taskWorkItem != null) throw new InvalidOperationException("Cannot change background task while it's running");
            _task = task;
            _taskWorkItem = AbortableThreadPool.QueueUserWorkItem(RunTask, new TaskStatus());
#endif
        }

        /// <summary>
        /// Blocks until the task finishes, ties up loose ends and then forgets all about the task.
        /// There will be no blocking if <see cref="TaskCompleted"/> is <c>true</c>.
        /// </summary>
        public void FinishTask()
        {
#if BROWSER
            if (!_browserTaskRunning)
                throw new InvalidOperationException("There is no background task to finish");
            if (!_browserTaskCompleted)
                throw new InvalidOperationException("The browser task has not completed");
            if (_browserTaskException != null)
                throw new ApplicationException("Exception thrown during background task", _browserTaskException);
            ResetTask();
#else
            if (_taskWorkItem == null)
                throw new InvalidOperationException("There is no background task to finish");
            var status = (TaskStatus)_taskWorkItem.State;

            // Block until the task completes.
            while (!status.IsCompleted) System.Threading.Thread.Sleep(0);

            if (status.Exception != null)
                throw new ApplicationException("Exception thrown during background task", status.Exception);
            ResetTask();
#endif
        }

        public void AbortTask()
        {
#if BROWSER
            if (!_browserTaskRunning) throw new InvalidOperationException("There is no background task to abort");
            ResetTask();
#else
            if (_taskWorkItem == null) throw new InvalidOperationException("There is no background task to abort");
            AbortableThreadPool.Abort(_taskWorkItem, true);
            ResetTask();
#endif
        }

        private void ResetTask()
        {
#if BROWSER
            _browserTaskRunning = false;
            _browserTaskCompleted = false;
            _browserTaskException = null;
#else
            _taskWorkItem = null;
#endif
            _task = null;
        }

        /// <summary>
        /// Runs the task, handling exceptions. This method is to be run in a background thread.
        /// </summary>
#if !BROWSER
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
#endif
    }
}
