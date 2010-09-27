using System.Net;
using System.Net.Sockets;
using AW2.Net.ConnectionUtils;
using AW2.Net.Messages;
using AW2.Helpers;

namespace AW2.Net.Connections
{
    /// <summary>
    /// A network connection to a game server.
    /// </summary>
    public class GameServerConnection : Connection
    {
        /// <summary>
        /// Creates a new connection to a game server.
        /// </summary>
        /// <param name="tcpSocket">An opened TCP socket to the remote host. The
        /// created connection owns the socket and will dispose of it.</param>
        public GameServerConnection(Socket tcpSocket, IPEndPoint remoteUDPEndPoint)
            : base(tcpSocket)
        {
            Name = "Game Server Connection " + ID;
            RemoteUDPEndPoint = remoteUDPEndPoint;
        }

        protected override void DisposeImpl(bool error)
        {
            AssaultWingCore.Instance.StopClient("Connection to server lost");
            base.DisposeImpl(error);
        }
    }
}
