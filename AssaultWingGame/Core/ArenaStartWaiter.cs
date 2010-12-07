using System;
using System.Collections.Generic;
using System.Linq;
using AW2.Helpers;
using AW2.Net.Connections;
using AW2.Net.MessageHandling;
using AW2.Net.Messages;

namespace AW2.Core
{
    public class ArenaStartWaiter : IDisposable
    {
        private bool _disposed;
        private IEnumerable<Connection> _connections;
        private List<int> _readyIDs;

        public bool IsEverybodyReady
        {
            get
            {
                CheckDisposed();
                return true;
            }
        }

        public ArenaStartWaiter(IEnumerable<Connection> connections)
        {
            _connections = connections;
            _readyIDs = new List<int>();
        }

        public void BeginWait()
        {
            CheckDisposed();
        }

        /// <summary>
        /// Finishes waiting. To be called when <see cref="IsFinished"/> is true.
        /// Returns the amount of time to wait before starting the arena.
        /// </summary>
        public TimeSpan EndWait()
        {
            CheckDisposed();
            var maxDelay = _connections.Count() == 0 ? TimeSpan.Zero : _connections.Max(conn => conn.PingInfo.PingTime).Divide(2);
            foreach (var conn in _connections)
            {
                conn.PingInfo.IsMeasuringFreezed = false;
                var startDelay = maxDelay - conn.PingInfo.PingTime.Divide(2);
                conn.Send(new ArenaStartRequest());
            }
            Dispose();
            return maxDelay;
        }

        public void Dispose()
        {
            _disposed = true;
        }

        private void CheckDisposed()
        {
            if (_disposed) throw new InvalidOperationException("This object has been disposed");
        }
    }
}
