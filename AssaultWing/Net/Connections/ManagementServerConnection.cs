using System.Net.Sockets;

namespace AW2.Net.Connections
{
    /// <summary>
    /// A network connection to a management server.
    /// </summary>
    public class ManagementServerConnection : Connection
    {
        /// <summary>
        /// Creates a new connection to a management server.
        /// </summary>
        /// <param name="socket">An opened socket to the remote host. The
        /// created connection owns the socket and will dispose of it.</param>
        public ManagementServerConnection(Socket socket)
            : base(socket)
        {
            Name = "Management Server Connection " + ID;
        }

        /// <summary>
        /// Performs the actual diposing.
        /// </summary>
        /// <param name="error">If <c>true</c> then an internal error
        /// has occurred.</param>
        protected override void DisposeImpl(bool error)
        {
            base.DisposeImpl(error);
            throw new System.NotImplementedException("ManagementServerConnection.Dispose() not implemented");
        }
    }
}
