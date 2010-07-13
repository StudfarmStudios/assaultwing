using System;
using System.Threading;

namespace AW2.Helpers
{
    // Snatched from Peter Ritchie's MVP Blog on 2010-03-18.
    // http://msmvps.com/blogs/peterritchie/archive/2006/10/13/_2700_System.Threading.Thread.Suspend_280029002700_-is-obsolete_3A00_-_2700_Thread.Suspend-has-been-deprecated_2E002E002E00_.aspx
    // Later modified from the original.
    public abstract class SuspendableThread
    {
        #region Data

        private ManualResetEvent suspendChangedEvent = new ManualResetEvent(false);
        private ManualResetEvent terminateEvent = new ManualResetEvent(false);
        private long suspended;
        private Thread thread;
        private ThreadState failsafeThreadState = ThreadState.Unstarted;

        #endregion Data

        public string Name { get; private set; }

        public SuspendableThread(string name)
        {
            Name = name;
        }

        private void ThreadEntry()
        {
            failsafeThreadState = ThreadState.Stopped;
            OnDoWork();
        }

        protected abstract void OnDoWork();

        #region Protected methods

        protected bool SuspendIfNeeded()
        {
            bool suspendEventChanged = suspendChangedEvent.WaitOne(0, true);
            if (suspendEventChanged)
            {
                bool needToSuspend = Interlocked.Read(ref suspended) != 0;
                suspendChangedEvent.Reset();
                if (needToSuspend)
                {
                    /// Suspending...
                    if (1 == WaitHandle.WaitAny(new WaitHandle[] { suspendChangedEvent, terminateEvent }))
                        return true;
                    /// ...Waking
                }
            }
            return false;
        }

        protected bool HasTerminateRequest()
        {
            return terminateEvent.WaitOne(0, true);
        }

        #endregion Protected methods

        #region Public methods

        public void Start()
        {
            thread = new Thread(new ThreadStart(ThreadEntry));
            thread.Name = Name;

            // make sure this thread won't be automatically
            // terminated by the runtime when the
            // application exits
            thread.IsBackground = false;

            thread.Start();
        }

        public void Join()
        {
            if (thread != null) thread.Join();
        }

        public bool Join(int milliseconds)
        {
            if (thread != null) return thread.Join(milliseconds);
            return true;

        }

        /// <remarks>Not supported in .NET Compact Framework</remarks>
        public bool Join(TimeSpan timeSpan)
        {
            if (thread != null) return thread.Join(timeSpan);
            return true;
        }

        public void Terminate()
        {
            terminateEvent.Set();
        }

        public void TerminateAndWait()
        {
            terminateEvent.Set();
            thread.Join();
        }

        public void Suspend()
        {
            while (1 != Interlocked.Exchange(ref suspended, 1)) { }
            suspendChangedEvent.Set();
        }

        public void Resume()
        {
            while (0 != Interlocked.Exchange(ref suspended, 0)) { }
            suspendChangedEvent.Set();
        }

        public ThreadState ThreadState
        {
            get
            {
                if (null != thread) return thread.ThreadState;
                return failsafeThreadState;
            }
        }

        #endregion
    }
}
