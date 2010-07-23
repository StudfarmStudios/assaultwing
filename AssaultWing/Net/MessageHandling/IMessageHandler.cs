using System;
using System.Collections.Generic;
using AW2.Net.Connections;

namespace AW2.Net.MessageHandling
{
    public abstract class IMessageHandler
    {
        public enum SourceType { Client, Server };

        public abstract bool Disposed { get; protected set; }
        public abstract void Dispose();
        public abstract void HandleMessages();

        protected static IEnumerable<Connection> GetConnections(SourceType source)
        {
            switch (source)
            {
                case SourceType.Client: return AssaultWing.Instance.NetworkEngine.GameClientConnections;
                case SourceType.Server: return new Connection[] { AssaultWing.Instance.NetworkEngine.GameServerConnection };
                default: throw new ApplicationException("Invalid SourceType " + source);
            }
        }
    }
}
