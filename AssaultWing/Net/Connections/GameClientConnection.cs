using System.Net.Sockets;
using AW2.Core;
using AW2.Net.ConnectionUtils;
using AW2.Net.Messages;

namespace AW2.Net.Connections
{
    /// <summary>
    /// A network connection to a game client.
    /// </summary>
    public class GameClientConnection : Connection
    {
        public GameClientStatus ConnectionStatus { get; set; }

        /// <summary>
        /// Creates a new connection to a game client.
        /// </summary>
        /// <param name="tcpSocket">An opened TCP socket to the remote host. The
        /// created connection owns the socket and will dispose of it.</param>
        public GameClientConnection(AssaultWing game, Socket tcpSocket)
            : base(game, tcpSocket)
        {
            Name = "Game Client Connection " + ID;
            ConnectionStatus = new GameClientStatus();
        }

        protected override void DisposeImpl(bool error)
        {
            Game.NetworkEngine.DropClient(ID);
            base.DisposeImpl(error);
        }
    }
}
