using System;
using System.Collections.Generic;
using System.Linq;
using AW2.Net;
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
                return _connections.All(conn => _readyIDs.Contains(conn.ID));
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
            MessageHandlers.ActivateHandlers(MessageHandlers.GetServerArenaStartHandlers(_readyIDs.Add));
            foreach (var conn in _connections) conn.Send(new ArenaStartRequest());
        }

        public void EndWait()
        {
            CheckDisposed();
            Dispose();
        }

        public void Dispose()
        {
            _disposed = true;
            MessageHandlers.DeactivateHandlers(MessageHandlers.GetServerArenaStartHandlers(null));
        }

        private void CheckDisposed()
        {
            if (_disposed) throw new InvalidOperationException("This object has been disposed");
        }
    }
}
