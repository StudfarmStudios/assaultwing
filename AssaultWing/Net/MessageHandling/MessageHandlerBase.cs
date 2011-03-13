using System;
using System.Collections.Generic;
using AW2.Core;
using AW2.Net.Connections;

namespace AW2.Net.MessageHandling
{
    public abstract class MessageHandlerBase
    {
        public enum SourceType { Client, Server, Management };

        public bool Disposed { get; private set; }
        private SourceType Source { get; set; }

        public MessageHandlerBase(SourceType source)
        {
            Source = source;
        }

        public void Dispose()
        {
            Disposed = true;
        }

        public void HandleMessages()
        {
            if (Disposed) throw new ObjectDisposedException("MessageHandlerBase");
            foreach (var connection in GetConnections(Source)) HandleMessagesImpl(connection);
        }

        protected abstract void HandleMessagesImpl(Connection connection);

        private static IEnumerable<Connection> GetConnections(SourceType source)
        {
            switch (source)
            {
                case SourceType.Client: return AssaultWing.Instance.NetworkEngine.GameClientConnections;
                case SourceType.Server: return new Connection[] { AssaultWing.Instance.NetworkEngine.GameServerConnection };
                case SourceType.Management: return new Connection[] { AssaultWing.Instance.NetworkEngine.ManagementServerConnection };
                default: throw new ApplicationException("Invalid SourceType " + source);
            }
        }
    }
}
