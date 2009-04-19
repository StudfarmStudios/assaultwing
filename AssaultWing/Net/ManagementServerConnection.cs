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
        /// Performs the actual diposing.
        /// </summary>
        protected override void DisposeImpl()
        {
            base.DisposeImpl();
            throw new System.NotImplementedException("ManagementServerConnection.Dispose() not implemented");
        }
    }
}
