using System;

namespace AW2.Net.MessageHandling
{
    public abstract class IMessageHandler
    {
        public enum SourceType { Client, Server };

        public abstract bool Disposed { get; protected set; }
        public abstract void Dispose();
        public abstract void HandleMessages();

        protected static AW2.Net.Connections.IConnection GetConnection(SourceType source)
        {
            switch (source)
            {
                case SourceType.Client: return AssaultWing.Instance.NetworkEngine.GameClientConnections;
                case SourceType.Server: return AssaultWing.Instance.NetworkEngine.GameServerConnection;
                default: throw new ApplicationException("Invalid SourceType " + source);
            }
        }
    }
}
