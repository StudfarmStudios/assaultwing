using System.Net.Sockets;

namespace AW2.Net
{
    /// <summary>
    /// A network connection to a game client.
    /// </summary>
    public class GameClientConnection : Connection
    {
        /// <summary>
        /// Creates a new connection to a game client.
        /// </summary>
        /// <param name="socket">An opened socket to the remote host. The
        /// created connection owns the socket and will dispose of it.</param>
        public GameClientConnection(Socket socket)
            : base(socket)
        {
            Name = "Game Client Connection " + ID;
        }

        /// <summary>
        /// Performs the actual diposing.
        /// </summary>
        /// <param name="error">If <c>true</c> then an internal error
        /// has occurred.</param>
        protected override void DisposeImpl(bool error)
        {
            // On internal error, notify the game instance.
            AssaultWing.Instance.NetworkEngine.DropClient(ID, error);
            base.DisposeImpl(error);
        }
    }
}
