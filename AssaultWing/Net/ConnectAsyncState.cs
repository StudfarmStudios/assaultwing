using System;
using System.Net.Sockets;

namespace AW2.Net
{
    /// <summary>
    /// The state of an asynchronous connection attempt (incoming or outgoing).
    /// </summary>
    public class ConnectAsyncState
    {
        public Socket Socket { get; set; }
        public ConnectAsyncState(Socket socket)
        {
            Socket = socket;
        }

        /// <summary>
        /// Callback implementation for accepting an incoming connection.
        /// </summary>
        /// <param name="asyncResult">The result of the asynchronous operation.</param>
        /// <param name="reportResult">Connection result reporting delegate.</param>
        public static void ConnectionAttemptCallback(IAsyncResult asyncResult, Func<Connection> createConnection, Action<Result<Connection>> reportResult)
        {
            ConnectAsyncState state = (ConnectAsyncState)asyncResult.AsyncState;
            try
            {
                var newConnection = createConnection();
                reportResult(new Result<Connection>(newConnection));
            }
            catch (Exception e)
            {
                if (e is ObjectDisposedException && ((ObjectDisposedException)e).ObjectName == "System.Net.Sockets.Socket")
                {
                    // This accept callback was triggered by the closing server socket.
                }
                else
                    reportResult(new Result<Connection>(e));
            }
        }
    }
}
