using System;
using System.Collections.Generic;
using System.Linq;
using AW2.Net;
using AW2.Net.Messages;

namespace AW2.Core
{
    public class ArenaStartWaiter : IDisposable
    {
        private bool _disposed;
        private MultiConnection _connections;
        private List<int> _readyIDs;

        public bool IsEverybodyReady
        {
            get
            {
                CheckDisposed();
                return _connections.Connections.All(conn => _readyIDs.Contains(conn.Id));
            }
        }

        public ArenaStartWaiter(MultiConnection connections)
        {
            _connections = connections;
            _readyIDs = new List<int>();
        }

        public void BeginWait()
        {
            CheckDisposed();
            var handlers = MessageHandlers.GetServerArenaStartHandlers(_connections, (id) => _readyIDs.Add(id));
            MessageHandlers.ActivateHandlers(handlers);
            _connections.Send(new ArenaStartRequest());
        }

        public void EndWait()
        {
            CheckDisposed();
            Dispose();
        }

        public void Dispose()
        {
            _disposed = true;
            MessageHandlers.DeactivateHandlers(MessageHandlers.GetServerArenaStartHandlers(null, null));
        }

        private void CheckDisposed()
        {
            if (_disposed) throw new InvalidOperationException("This object has been disposed");
        }
    }
}
