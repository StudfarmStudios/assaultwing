using System.Net.Sockets;
using AW2.Core;
using AW2.Net.ConnectionUtils;
using AW2.Net.Messages;

namespace AW2.Net.Connections
{
    /// <summary>
    /// A network connection to a game client.
    /// </summary>
    public class GameClientConnectionRaw : ConnectionRaw, GameClientConnection
    {
        public GameClientStatus ConnectionStatus { get; set; }

        /// <summary>
        /// Creates a new connection to a game client.
        /// </summary>
        /// <param name="tcpSocket">An opened TCP socket to the remote host. The
        /// created connection owns the socket and will dispose of it.</param>
        public GameClientConnectionRaw(AssaultWingCore game, Socket tcpSocket)
            : base(game, tcpSocket)
        {
            Name = string.Format("Game Client {0} ({1})", ID, RemoteTCPEndPoint.Address);
            ConnectionStatus = new GameClientStatus();
        }

        public override void Send(Message message)
        {
            // UDP messages are not important, just skip them if we're not active yet.
            // TCP messages are important, so let them get queued; just delay queue flushing (done in Update).
            if (ConnectionStatus.State == GameClientStatus.StateType.Active || message.SendType == MessageSendType.TCP)
                base.Send(message);
        }

        public override void Update()
        {
            // Postpone flushing TCP send buffers until we are active.
            if (ConnectionStatus.State == GameClientStatus.StateType.Active)
                base.Update();
        }

        protected override void DisposeImpl(bool error)
        {
            Game.NetworkEngine.DropClient(this);
            base.DisposeImpl(error);
        }
    }
}
