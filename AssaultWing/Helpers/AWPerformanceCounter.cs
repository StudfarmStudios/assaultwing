using System.Diagnostics;

namespace AW2.Helpers
{
    /// <summary>
    /// Wrapper for <see cref="System.Diagnostics.PerformanceCounter"/>.
    /// Contains the real counter as <see cref="AWPerformanceCounter.Impl"/>.
    /// This is <c>null</c> by default and can be assigned a real counter.
    /// Operations performed on the wrapper are delegated to the real counter.
    /// If no real counter has been assigned, all operations are dummies.
    /// </summary>
    public struct AWPerformanceCounter
    {
        /// <summary>
        /// The system implementation of the performance counter, or 
        /// <c>null</c> if all operations are dummies.
        /// </summary>
        public PerformanceCounter Impl { get; set; }

        /// <summary>
        /// Increments the associated performance counter by one through an efficient atomic operation.
        /// </summary>
        /// <returns>The incremented counter value.</returns>
        public long Increment()
        {
            if (Impl != null) return Impl.Increment();
            return 0;
        }
    }
}
