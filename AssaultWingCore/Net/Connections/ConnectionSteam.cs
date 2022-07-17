using AW2.Core;
using Steamworks;
using AW2.Helpers;

namespace AW2.Net.Connections
{
    /// <summary>
    /// A connection to a remote host over Steam network. Communication between 
    /// the local and remote host is done by messages. 
    /// </summary>
    /// <remarks>
    /// Connection operates asynchronously. Both creation of connections and 
    /// sending messages via connections are done asynchronously. Therefore 
    /// their result is not known by the time the corresponding method call returns. 
    /// When results of such actions eventually arrive (as either success or 
    /// failure), they are added to corresponding queues.
    /// It is up to the client program to read the results from the queues.
    /// This can be done handily in the client program main loop. If such 
    /// a loop is not available, or for other reasons, the client program 
    /// can hook up events that notify of finished asynchronous operations.
    /// Such queues exist for connection attempts (static), received messages 
    /// (for each connection) and general error conditions (for each connection).
    /// 
    /// This class is thread safe.
    /// </remarks>
    public abstract class ConnectionSteam : ConnectionBase, IDisposable
    {
        public HSteamNetConnection Handle { get; init; }
        public SteamNetConnectionInfo_t Info { get; set; }

        protected ConnectionSteam(AssaultWingCore game, HSteamNetConnection handle, SteamNetConnectionInfo_t info)
            : base(game)
        {
            Handle = handle;
            Info = info;
        }

        public override void QueueError(string message)
        {
            throw new NotImplementedException();
        }

        public override void Send(Message message)
        {
            if (IsDisposed) return;
            switch (message.SendType)
            {
                case MessageSendType.TCP: 
                    Log.Write($"TODO: Send reliable {message.Type}");
                    break;
                case MessageSendType.UDP:
                    Log.Write($"TODO: Send unreliable {message.Type}");
                    break;
                default: throw new MessageException("Unknown send type " + message.SendType);
            }
        }

        /// <summary>
        /// Performs the actual disposing.
        /// </summary>
        /// <param name="error">If <c>true</c> then an internal error has occurred.</param>
        override protected void DisposeImpl(bool error)
        {
            SteamNetworkingSockets.CloseConnection(Handle, 0, "Disposed", true);
            DisposeId();
        }
    }
}