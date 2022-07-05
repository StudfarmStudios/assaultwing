using System.Net;
using System.Net.Sockets;
using AW2.Core;
using AW2.Helpers;
using AW2.Net.ConnectionUtils;
using AW2.Net.Messages;

namespace AW2.Net.Connections
{
    /// <summary>
    /// A network connection to a game server.
    /// </summary>
    public class GameServerConnectionRaw : ConnectionRaw
    {
        /// <summary>
        /// Creates a new connection to a game server.
        /// </summary>
        /// <param name="tcpSocket">An opened TCP socket to the remote host. The
        /// created connection owns the socket and will dispose of it.</param>
        public GameServerConnectionRaw(AssaultWingCore game, Socket tcpSocket)
            : base(game, tcpSocket)
        {
            Name = string.Format("Game Server {0} ({1})", ID, RemoteTCPEndPoint.Address);
        }

        protected override void DisposeImpl(bool error)
        {
            if (error) Game.NetworkingErrors.Enqueue("Connection to server lost.");
            base.DisposeImpl(error);
        }
    }
}
