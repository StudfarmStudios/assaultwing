using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Microsoft.Xna.Framework;
using AW2.Core;
using AW2.Game;
using AW2.Helpers;
using AW2.Net.MessageHandling;
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

        private enum ConnectionType
        {
            ManagementServer,
            GameServer,
            GameClient
        }

        #endregion Type definitions

        #region Fields

        private const string NETWORK_TRACE_FILE = "AWnetwork.log";
        private const int TCP_CONNECTION_PORT = 'A' * 256 + 'W';

        /// <summary>
        /// Network connection to the management server, 
        /// or <c>null</c> if no such live connection exists.
        /// </summary>
        private PingedConnection _managementServerConnection = null; // HACK: assignment to avoid compiler warning

        /// <summary>
        /// Network connection to the game server of the current game session, 
        /// or <c>null</c> if no such live connection exists 
        /// (including the case that we are the game server).
        /// </summary>
        private PingedConnection _gameServerConnection;

        /// <summary>
        /// Network connections to game clients. Nonempty only on a game server.
        /// </summary>
        private MultiConnection _gameClientConnections;

        /// <summary>
        /// Clients to be removed from <c>clientConnections</c>.
        /// </summary>
        private List<IConnection> _removedClientConnections;

        /// <summary>
        /// Handler of connection results for client that is connecting to a game server.
        /// </summary>
        private Action<Result<Connection>> _startClientConnectionHandler;

        /// <summary>
        /// Handler of connection results for server that is listening for game client connections.
        /// </summary>
        private Action<Result<Connection>> _startServerConnectionHandler;

        #endregion Fields

        #region Constructor

        /// <summary>
        /// Creates a network engine for a game.
        /// </summary>
        /// <param name="game">The game.</param>
        public NetworkEngine(Microsoft.Xna.Framework.Game game)
            : base(game)
        {
            _gameClientConnections = new MultiConnection { Name = "Game Client Connections" };
            _removedClientConnections = new List<IConnection>();
            MessageHandlers = new List<IMessageHandler>();
        }

        #endregion Constructor

        #region Public interface

        /// <summary>
        /// The handlers of network messages.
        /// </summary>
        public List<IMessageHandler> MessageHandlers { get; private set; }

        /// <summary>
        /// Are we connected to a game server.
        /// </summary>
        public bool IsConnectedToGameServer { get { return _gameServerConnection != null; } }

        /// <summary>
        /// Are we connected to the management server.
        /// </summary>
        public bool IsConnectedToManagementServer { get { return _managementServerConnection != null; } }

        /// <summary>
        /// Connections to game clients.
        /// </summary>
        public MultiConnection GameClientConnections
        {
            get
            {
                if (_gameClientConnections == null) throw new InvalidOperationException("No connections to game clients");
                return _gameClientConnections;
            }
        }

        /// <summary>
        /// Connection to the game server.
        /// </summary>
        public IConnection GameServerConnection
        {
            get
            {
                if (_gameServerConnection == null) throw new InvalidOperationException("No connection to game server");
                return _gameServerConnection;
            }
        }

        /// <summary>
        /// Connection to the management server.
        /// </summary>
        public IConnection ManagementServerConnection
        {
            get
            {
                if (_managementServerConnection == null) throw new InvalidOperationException("No connection to management server");
                return _managementServerConnection;
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
            _startServerConnectionHandler = connectionHandler;
            ConnectionAttemptListener.Instance.StartListening(TCP_CONNECTION_PORT);
        }

        /// <summary>
        /// Turns this game server into a standalone game instance and disposes of
        /// any connections to game clients.
        /// </summary>
        public void StopServer()
        {
            Log.Write("Server stops listening");
            MessageHandlers.Clear();
            ConnectionAttemptListener.Instance.StopListening();
            _gameClientConnections.Dispose();
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
            _startClientConnectionHandler = connectionHandler;
            IPAddress serverIp;
            if (!System.Net.IPAddress.TryParse(serverAddress, out serverIp))
                throw new ArgumentException("Not a valid IP address: " + serverAddress);
            Connection.Connect(serverIp, TCP_CONNECTION_PORT);
        }

        /// <summary>
        /// Turns this game client into a standalone game instance by disconnecting
        /// from the game server.
        /// </summary>
        public void StopClient()
        {
            Log.Write("Client closes connection");
            MessageHandlers.Clear();
            Connection.CancelConnect();
            if (_gameServerConnection != null)
            {
                _gameServerConnection.BaseConnection.Dispose();
                _gameServerConnection = null;
            }
        }

        /// <summary>
        /// Drops the connection to a game client. To be called only
        /// as the game server.
        /// </summary>
        /// <param name="error">If true, client is being dropped due to an error condition.</param>
        public void DropClient(int connectionId, bool error)
        {
            if (AssaultWing.Instance.NetworkMode != NetworkMode.Server)
                throw new InvalidOperationException("Cannot drop client in mode " + AssaultWing.Instance.NetworkMode);

            var connection = GameClientConnections[connectionId];
            _removedClientConnections.Add(connection);

            // Remove the client's players.
            if (error)
            {
                List<string> droppedPlayerNames = new List<string>();
                foreach (var player in AssaultWing.Instance.DataEngine.Spectators)
                    if (player.ConnectionId == connection.Id)
                        droppedPlayerNames.Add(player.Name);
                string message = string.Join(" and ", droppedPlayerNames.ToArray()) + " dropped out";
                foreach (var player in AssaultWing.Instance.DataEngine.Players)
                    if (!player.IsRemote)
                        player.SendMessage(message);
            }
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
                if (_gameServerConnection == null)
                    throw new InvalidOperationException("Cannot ping server without connection");
                return _gameServerConnection.PingTime;
            }
        }

        /// <summary>
        /// Offset of game time on the server compared to this game instance.
        /// </summary>
        /// Adding the offset to the server game time gives our game time.
        public TimeSpan ServerGameTimeOffset
        {
            get
            {
                if (_gameServerConnection == null)
                    throw new InvalidOperationException("Cannot count server game time offset without connection");
                return _gameServerConnection.RemoteGameTimeOffset;
            }
        }

        /// <summary>
        /// Round-trip ping time to a game client.
        /// </summary>
        public TimeSpan GetClientPingTime(int connectionId)
        {
            var connection = GameClientConnections[connectionId] as PingedConnection;
            if (connection == null) throw new InvalidOperationException("Cannot ping client without PingedConnection");
            return connection.PingTime;
        }

        /// <summary>
        /// Offset of game time on a client compared to this server instance.
        /// </summary>
        /// Adding the offset to the client game time gives our game time.
        public TimeSpan GetClientGameTimeOffset(int connectionId)
        {
            var connection = GameClientConnections[connectionId] as PingedConnection;
            if (connection == null) throw new InvalidOperationException("Cannot count client game time offset without PingedConnection");
            return connection.RemoteGameTimeOffset;
        }

        /// <summary>
        /// Returns the amount of game time elapsed since the message was sent.
        /// This takes into account the shift in game time between different game instances.
        /// </summary>
        public TimeSpan GetMessageAge(GameplayMessage message)
        {
            return AssaultWing.Instance.DataEngine.ArenaTotalTime
                - message.TotalGameTime
                - ((PingedConnection)GetConnection(message.ConnectionId)).RemoteGameTimeOffset;
        }

        #endregion Public interface

        #region GameComponent methods

        public override void Initialize()
        {
            base.Initialize();
            if (File.Exists(NETWORK_TRACE_FILE))
            {
                Log.Write("Deleting old network trace file " + NETWORK_TRACE_FILE);
                File.Delete(NETWORK_TRACE_FILE);
            }
        }

        public override void Update(GameTime gameTime)
        {
            if (ConnectionAttemptListener.Instance.IsListening) ConnectionAttemptListener.Instance.Update();

            // Handle established connections.
            ConnectionAttemptListener.Instance.ConnectionResults.Do(queue =>
            {
                while (queue.Count > 0)
                {
                    var result = queue.Dequeue();
                    if (result.Successful) _gameClientConnections.Connections.Add(new PingedConnection(result.Value));
                    _startServerConnectionHandler(result);
                }
            });
            Connection.ConnectionResults.Do(queue =>
            {
                while (queue.Count > 0)
                {
                    var result = queue.Dequeue();
                    if (result.Successful) _gameServerConnection = new PingedConnection(result.Value);
                    _startClientConnectionHandler(result);
                }
            });

            // Update ping time measurements.
            ForEachConnection(connection => connection.Update());

            foreach (var handler in MessageHandlers) if (!handler.Disposed) handler.HandleMessages();
            RemoveDisposedMessageHandlers();

            // Handle occurred errors.
            ForEachConnection(connection => connection.HandleErrors());

            // Finish removing dropped client connections.
            foreach (PingedConnection connection in _removedClientConnections)
                _gameClientConnections.Connections.Remove(connection);
            _removedClientConnections.Clear();

#if DEBUG
            // Look for unhandled messages.
            Type lastMessageType = null; // to avoid flooding log messages
            IConnection lastConnection = null;
            ForEachConnection(connection => connection.Messages.Prune(TimeSpan.FromSeconds(30), message =>
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

        protected override void Dispose(bool disposing)
        {
            _gameClientConnections.Dispose();
            base.Dispose(disposing);
        }

        #endregion GameComponent methods

        #region Private methods

        private void RemoveDisposedMessageHandlers()
        {
            MessageHandlers = MessageHandlers.Except(MessageHandlers.Where(handler => handler.Disposed)).ToList();
        }

        private void ForEachConnection(Action<IConnection> action)
        {
            if (_managementServerConnection != null)
                action(_managementServerConnection);
            if (_gameServerConnection != null)
                action(_gameServerConnection);
            if (_gameClientConnections != null)
                action(_gameClientConnections);
        }

        private IConnection GetConnection(int connectionId)
        {
            IConnection result = null;
            Action<IConnection> finder = null;
            finder = conn =>
            {
                if (conn is MultiConnection)
                    foreach (var conn2 in ((MultiConnection)conn).Connections)
                        finder(conn2);
                else
                    if (conn.Id == connectionId)
                        result = conn;
            };
            ForEachConnection(finder);
            if (result == null) throw new ArgumentException("Connection not found with ID " + connectionId);
            return result;
        }

        #endregion Private methods
    }
}
