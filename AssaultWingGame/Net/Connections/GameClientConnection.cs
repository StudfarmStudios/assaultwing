using System.Net.Sockets;
using AW2.Core;

namespace AW2.Net.Connections
{
    /// <summary>
    /// A network connection to a game client.
    /// </summary>
    public class GameClientConnection : Connection
    {
        public bool IsPlayingArena { get { return string.IsNullOrEmpty(PlayingArenaName); } }
        public string PlayingArenaName { get; set; }

        /// <summary>
        /// Creates a new connection to a game client.
        /// </summary>
        /// <param name="tcpSocket">An opened TCP socket to the remote host. The
        /// created connection owns the socket and will dispose of it.</param>
        public GameClientConnection(AssaultWing game, Socket tcpSocket)
            : base(game, tcpSocket)
        {
            Name = "Game Client Connection " + ID;
        }

        protected override void DisposeImpl(bool error)
        {
            if (error) Game.NetworkEngine.DropClient(ID, error);
            base.DisposeImpl(error);
        }
    }
}
