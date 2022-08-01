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
        public AWEndPointRaw[] RemoteEndPoints { get; private set; }
        public bool IsCancelled { get; private set; }

        /// <summary>
        /// Callback implementation for accepting an incoming connection.
        /// Returns the result (either a connection or an exception),
        /// or null if the connection attempt was cancelled.
        /// </summary>
        /// <param name="asyncResult">The result of the asynchronous operation.</param>
        public static Result<ConnectionRaw> ConnectionAttemptCallback(IAsyncResult asyncResult, Func<ConnectionRaw> createConnection)
        {
            Result<ConnectionRaw> result = null;
            var state = (ConnectAsyncState)asyncResult.AsyncState;
            if (!state.IsCancelled)
            {
                try
                {
                    var newConnection = createConnection();
                    result = new Result<ConnectionRaw>(newConnection);
                }
                catch (Exception e)
                {
                    result = new Result<ConnectionRaw>(e);
                }
            }
            return result;
        }

        public ConnectAsyncState(AssaultWingCore game, Socket[] sockets, AWEndPointRaw[] remoteEndPoints)
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
