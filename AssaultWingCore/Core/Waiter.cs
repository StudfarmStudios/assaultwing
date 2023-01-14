using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace AW2.Core
{
    public class Waiter
    {
        public static readonly TimeSpan PRECISION = TimeSpan.FromMilliseconds(PRECISION_MS);
        private const int PRECISION_MS = 1;
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
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                WinMm.timeBeginPeriod(PRECISION_MS);
            }
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
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                WinMm.timeEndPeriod(PRECISION_MS);
            }
        }
    }
}
