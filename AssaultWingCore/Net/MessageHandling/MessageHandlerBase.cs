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
            var net = AssaultWingCore.Instance.NetworkEngine;
            switch (source)
            {
                case SourceType.Client:
                    if (net.GameClientConnections != null)
                        foreach (var conn in net.GameClientConnections) yield return conn;
                    break;
                case SourceType.Server:
                    if (net.IsConnectedToGameServer) yield return net.GameServerConnection;
                    break;
                case SourceType.Management:
                    if (net.IsConnectedToManagementServer) yield return net.ManagementServerConnection;
                    break;
                default: throw new ApplicationException("Invalid SourceType " + source);
            }
        }
    }
}
