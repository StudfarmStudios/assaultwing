using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace AW2.Core
{
    public class Waiter
    {
        [DllImport("winmm.dll")]
        private static extern uint timeBeginPeriod(uint period);
        [DllImport("winmm.dll")]
        private static extern uint timeEndPeriod(uint period);

        private static Waiter g_instance;

        private bool _disposed;

        public static Waiter Instance
        {
            get
            {
                if (g_instance == null) g_instance = new Waiter();
                return g_instance;
            }
        }

        private Waiter()
        {
            timeBeginPeriod(1);
        }

        ~Waiter()
        {
            Dispose();
        }

        /// <summary>
        /// Sleeps a time period that is close to the specified time. Expected accuracy is +-1 ms.
        /// </summary>
        public void Sleep(TimeSpan timeout)
        {
            Thread.Sleep(timeout);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            timeEndPeriod(1);
        }
    }
}
