using System;

namespace AW2.Core
{
    /// <summary>
    /// A dummy implementation of gathering statistics. Game clients use this implementation.
    /// </summary>
    public class StatsBase : IDisposable
    {
        public virtual void Dispose() { }
        public virtual void Send(object obj) { }
    }
}
