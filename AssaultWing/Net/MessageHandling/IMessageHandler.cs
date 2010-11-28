using System;
using System.Collections.Generic;
using AW2.Core;
using AW2.Net.Connections;

namespace AW2.Net.MessageHandling
{
    public abstract class IMessageHandler
    {
        public enum SourceType { Client, Server, Management };

        public abstract bool Disposed { get; protected set; }
        public abstract void Dispose();
        public abstract void HandleMessages();

        protected static IEnumerable<Connection> GetConnections(SourceType source)
        {
            switch (source)
            {
                case SourceType.Client: return AssaultWingCore.Instance.NetworkEngine.GameClientConnections;
                case SourceType.Server: return new Connection[] { AssaultWingCore.Instance.NetworkEngine.GameServerConnection };
                case SourceType.Management: return new Connection[] { AssaultWingCore.Instance.NetworkEngine.ManagementServerConnection };
                default: throw new ApplicationException("Invalid SourceType " + source);
            }
        }
    }
}
