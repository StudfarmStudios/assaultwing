using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Microsoft.Xna.Framework;
using AW2.Game;
using AW2.Helpers;
using AW2.Net.Messages;

namespace AW2.Net
{
    /// <summary>
    /// Network engine. Takes care of communications between several
    /// Assault Wing instances over the Internet.
    /// </summary>
    /// <para>
    /// A game server can communicate with its game clients by sending
    /// multicast messages via <c>SendToClients</c> and receiving
    /// messages via <c>ReceiveFromClients</c>. Messages can be
    /// received by type, so each part of the game logic can poll for 
    /// messages that are relevant to it without interfering with
    /// other parts of the game logic. Each received message
    /// contains an identifier of the connection to the client who
    /// sent that message.
    /// </para><para>
    /// A game client can communicate with its game server by sending
    /// messages via <c>SendToServer</c> and receiving messages via
    /// <c>ReceiveFromServer</c>.
    /// </para><para>
    /// All game instances can have a connection to a game management
    /// server. This hasn't been implemented yet.
    /// </para><para>
    /// <see cref="NetworkEngine"/> reacts to incoming messages according
    /// to message handlers that other components register.
    /// </para>
    /// <seealso cref="Message.ConnectionId"/>
    public class NetworkEngine : GameComponent
    {
        #region Type definitions

        enum ConnectionType
        {
            ManagementServer,
            GameServer,
            GameClient
        }

        #endregion Type definitions

        #region Fields

        /// <summary>
        /// TCP connection port.
        /// </summary>
        int port = 'A' * 256 + 'W';

        /// <summary>
        /// Network connection to the management server, 
        /// or <c>null</c> if no such live connection exists.
        /// </summary>
        PingedConnection managementServerConnection = null; // HACK: assignment to avoid compiler warning

        /// <summary>
        /// Network connection to the game server of the current game session, 
        /// or <c>null</c> if no such live connection exists 
        /// (including the case that we are the game server).
        /// </summary>
        PingedConnection gameServerConnection;

        /// <summary>
        /// Network connections to game clients. Nonempty only on a game server.
        /// </summary>
        MultiConnection gameClientConnections;

        /// <summary>
        /// Clients to be removed from <c>clientConnections</c>.
        /// </summary>
        List<IConnection> removedClientConnections;

        /// <summary>
        /// Handler of connection results for client that is connecting to a game server.
        /// </summary>
        Action<Result<Connection>> startClientConnectionHandler;

        /// <summary>
        /// Handler of connection results for server that is listening for game client connections.
        /// </summary>
        Action<Result<Connection>> startServerConnectionHandler;

        #endregion Fields

        #region Constructor

        /// <summary>
        /// Creates a network engine for a game.
        /// </summary>
        /// <param name="game">The game.</param>
        public NetworkEngine(Microsoft.Xna.Framework.Game game)
            : base(game)
        {
            gameClientConnections = new MultiConnection { Name = "Game Client Connections" };
            removedClientConnections = new List<IConnection>();
            MessageHandlers = new List<IMessageHandler>();
        }

        #endregion Constructor

        #region Public interface

        /// <summary>
        /// The handlers of network messages.
        /// </summary>
        public IList<IMessageHandler> MessageHandlers { get; private set; }

        /// <summary>
        /// Are we connected to a game server.
        /// </summary>
        public bool IsConnectedToGameServer { get { return gameServerConnection != null; } }

        /// <summary>
        /// Are we connected to the management server.
        /// </summary>
        public bool IsConnectedToManagementServer { get { return managementServerConnection != null; } }

        /// <summary>
        /// Connections to game clients.
        /// </summary>
        public MultiConnection GameClientConnections
        {
            get
            {
                if (gameClientConnections == null) throw new InvalidOperationException("No connections to game clients");
                return gameClientConnections;
            }
        }

        /// <summary>
        /// Connection to the game server.
        /// </summary>
        public Connection GameServerConnection
        {
            get
            {
                if (gameServerConnection == null) throw new InvalidOperationException("No connection to game server");
                return gameServerConnection.BaseConnection;
            }
        }

        /// <summary>
        /// Connection to the management server.
        /// </summary>
        public Connection ManagementServerConnection
        {
            get
            {
                if (managementServerConnection == null) throw new InvalidOperationException("No connection to management server");
                return managementServerConnection.BaseConnection;
            }
        }

        /// <summary>
        /// Turns this game instance into a game server to whom other game instances
        /// can connect as game clients.
        /// </summary>
        /// <param name="connectionHandler">Handler of connection result.</param>
        public void StartServer(Action<Result<Connection>> connectionHandler)
        {
            Log.Write("Server starts listening");
            startServerConnectionHandler = connectionHandler;
            Connection.StartListening(port, "I listen");
        }

        /// <summary>
        /// Turns this game server into a standalone game instance and disposes of
        /// any connections to game clients.
        /// </summary>
        public void StopServer()
        {
            Log.Write("Server stops listening");
            Connection.StopListening();
            gameClientConnections.Dispose();
        }

        /// <summary>
        /// Turns this game instance into a game client by connecting to a game server.
        /// </summary>
        /// Poll <c>Connection.ConnectionResults</c> to find out when and if
        /// the connection was successfully estblished.
        /// <param name="serverAddress">Network address of the server.</param>
        /// <param name="connectionHandler">Handler of connection result.</param>
        public void StartClient(string serverAddress, Action<Result<Connection>> connectionHandler)
        {
            Log.Write("Client starts connecting");
            startClientConnectionHandler = connectionHandler;
            IPAddress serverIp;
            if (!System.Net.IPAddress.TryParse(serverAddress, out serverIp))
                throw new ArgumentException("Not a valid IP address: " + serverAddress);
            Connection.Connect(serverIp, port, "I connect");
        }

        /// <summary>
        /// Turns this game client into a standalone game instance by disconnecting
        /// from the game server.
        /// </summary>
        public void StopClient()
        {
            Log.Write("Client closes connection");
            if (gameServerConnection != null)
            {
                gameServerConnection.BaseConnection.Dispose();
                gameServerConnection = null;
            }
        }

        /// <summary>
        /// Drops the connection to a game client. To be called only
        /// as the game server.
        /// </summary>
        public void DropClient(int connectionId)
        {
            if (AssaultWing.Instance.NetworkMode != NetworkMode.Server)
                throw new InvalidOperationException("Cannot drop client in mode " + AssaultWing.Instance.NetworkMode);

            var connection = GameClientConnections[connectionId];
            removedClientConnections.Add(connection);

            // Remove the client's players.
            List<string> droppedPlayerNames = new List<string>();
            foreach (var player in AssaultWing.Instance.DataEngine.Spectators)
                if (player.ConnectionId == connection.Id)
                    droppedPlayerNames.Add(player.Name);
            string message = string.Join(" and ", droppedPlayerNames.ToArray()) + " dropped out";
            foreach (var player in AssaultWing.Instance.DataEngine.Players)
                if (!player.IsRemote)
                    player.SendMessage(message);
            AssaultWing.Instance.DataEngine.Spectators.Remove(player => player.ConnectionId == connection.Id);
        }

        /// <summary>
        /// Returns the number of bytes waiting to be sent through the network.
        /// </summary>
        public int GetSendQueueSize()
        {
            int count = 0;
            ForEachConnection(connection =>
            {
                count += connection.GetSendQueueSize();
            });
            return count;
        }

        /// <summary>
        /// Round-trip ping time to the game server.
        /// </summary>
        public TimeSpan ServerPingTime
        {
            get
            {
                if (gameServerConnection == null)
                    throw new InvalidOperationException("Cannot ping server without connection");
                return gameServerConnection.PingTime;
            }
        }

        #endregion Public interface

        #region GameComponent methods

        /// <summary>
        /// Performs game logic.
        /// </summary>
        /// <param name="gameTime">Time elapsed since the last call to Update</param>
        public override void Update(GameTime gameTime)
        {
            // Handle established connections.
            Connection.ConnectionResults.Do(queue =>
            {
                while (queue.Count > 0)
                {
                    var result = queue.Dequeue();
                    if (result.Id == "I connect")
                    {
                        if (result.Successful)
                            gameServerConnection = new PingedConnection(result.Value);
                        startClientConnectionHandler(result);
                    }
                    if (result.Id == "I listen")
                    {
                        if (result.Successful)
                            gameClientConnections.Connections.Add(new PingedConnection(result.Value));
                        startServerConnectionHandler(result);
                    }
                }
            });

            // Update ping time measurements.
            ForEachConnection(connection => connection.Update());

            foreach (var handler in MessageHandlers)
                handler.HandleMessages();
            for (int i = MessageHandlers.Count - 1; i >= 0; --i)
                if (MessageHandlers[i].Disposed) MessageHandlers.RemoveAt(i);

            // Handle occurred errors.
            ForEachConnection(connection => connection.HandleErrors());

            // Finish removing dropped client connections.
            foreach (PingedConnection connection in removedClientConnections)
                gameClientConnections.Connections.Remove(connection);

#if DEBUG
            // Look for unhandled messages.
            Type lastMessageType = null; // to avoid flooding log messages
            IConnection lastConnection = null;
            ForEachConnection(connection => connection.Messages.Prune(TimeSpan.FromSeconds(10), message =>
            {
                if (lastMessageType != message.GetType() || lastConnection != connection)
                {
                    lastMessageType = message.GetType();
                    lastConnection = connection;
                    Log.Write("WARNING: Purging messages of type " + message.Type + " received from " + connection.Name);
                }
            }));
#endif
        }

        /// <summary>
        /// Releases the unmanaged resources used by the GameComponent 
        /// and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources;
        /// <c>false</c> to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            gameClientConnections.Dispose();
            base.Dispose(disposing);
        }

        #endregion GameComponent methods

        #region Private methods

        /// <summary>
        /// Performs an operation on each established connection.
        /// </summary>
        /// <param name="action">The action to perform.</param>
        void ForEachConnection(Action<IConnection> action)
        {
            if (managementServerConnection != null)
                action(managementServerConnection);
            if (gameServerConnection != null)
                action(gameServerConnection);
            if (gameClientConnections != null)
                action(gameClientConnections);
        }

        #endregion Private methods
    }
}
