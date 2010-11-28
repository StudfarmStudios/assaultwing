using System;
using System.Net.Sockets;
using AW2.Core;
using AW2.Net.Connections;

namespace AW2.Net.ConnectionUtils
{
    /// <summary>
    /// The state of an asynchronous connection attempt (incoming or outgoing).
    /// </summary>
    public class ConnectAsyncState
    {
        public AssaultWingCore Game { get; private set; }
        public Socket[] Sockets { get; private set; }
        public AWEndPoint[] RemoteEndPoints { get; private set; }
        public bool IsCancelled { get; private set; }

        /// <summary>
        /// Callback implementation for accepting an incoming connection.
        /// Returns the result (either a connection or an exception),
        /// or null if the connection attempt was cancelled.
        /// </summary>
        /// <param name="asyncResult">The result of the asynchronous operation.</param>
        public static Result<Connection> ConnectionAttemptCallback(IAsyncResult asyncResult, Func<Connection> createConnection)
        {
            Result<Connection> result = null;
            var state = (ConnectAsyncState)asyncResult.AsyncState;
            if (!state.IsCancelled)
            {
                try
                {
                    var newConnection = createConnection();
                    result = new Result<Connection>(newConnection);
                }
                catch (Exception e)
                {
                    if (e is ObjectDisposedException && ((ObjectDisposedException)e).ObjectName == "System.Net.Sockets.Socket")
                    {
                        // This accept callback was triggered by the closing server socket.
                    }
                    else
                        result = new Result<Connection>(e);
                }
            }
            return result;
        }

        public ConnectAsyncState(AssaultWingCore game, Socket[] sockets, AWEndPoint[] remoteEndPoints)
        {
            Game = game;
            Sockets = sockets;
            RemoteEndPoints = remoteEndPoints;
        }

        public void Cancel()
        {
            IsCancelled = true;
        }
    }
}
