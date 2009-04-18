using System.Net.Sockets;

namespace AW2.Net
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
            Name = "Management Server Connection " + Id;
        }

        /// <summary>
        /// Closes the connection and frees resources it has allocated.
        /// </summary>
        public override void Dispose()
        {
            if (IsDisposed) return;
            base.Dispose();
            throw new System.NotImplementedException("ManagementServerConnection.Dispose() not implemented");
        }
    }
}
