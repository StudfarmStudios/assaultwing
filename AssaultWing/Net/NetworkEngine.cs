using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using AW2.Core;
using AW2.Game;
using AW2.Helpers;
using AW2.Net.Connections;
using AW2.Net.ConnectionUtils;
using AW2.Net.ManagementMessages;
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
    /// <seealso cref="Message.ConnectionID"/>
    public class NetworkEngine : AWGameComponent
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

        public const int TCP_CONNECTION_PORT = 'A' * 256 + 'W';
        private const int MANAGEMENT_SERVER_PORT_DEFAULT = 'A' * 256 + 'W';
        private const string NETWORK_TRACE_FILE = "AWnetwork.log";

        private AssaultWing _game;

        /// <summary>
        /// Network connection to the management server, 
        /// or <c>null</c> if no such live connection exists.
        /// </summary>
        private Connection _managementServerConnection;

        /// <summary>
        /// Network connection to the game server of the current game session, 
        /// or <c>null</c> if no such live connection exists 
        /// (including the case that we are the game server).
        /// </summary>
        private Connection _gameServerConnection;

        /// <summary>
        /// Network connections to game clients. Nonempty only on a game server.
        /// </summary>
        private List<GameClientConnection> _gameClientConnections;

        /// <summary>
        /// Clients to be removed from <c>clientConnections</c>.
        /// </summary>
        private List<GameClientConnection> _removedClientConnections;

        /// <summary>
        /// Handler of connection results for client that is connecting to a game server.
        /// </summary>
        private Action<Result<Connection>> _startClientConnectionHandler;

        /// <summary>
        /// Handler of connection results for server that is listening for game client connections.
        /// </summary>
        private Action<Result<Connection>> _startServerConnectionHandler;

        private ThreadSafeWrapper<List<Tuple<Message, IPEndPoint>>> _udpMessagesToHandle;
        private ConnectionAttemptListener _connectionAttemptListener;

        #endregion Fields

        #region Constructor

        public NetworkEngine(AssaultWing game, int updateOrder)
            : base(game, updateOrder)
        {
            _game = game;
            _gameClientConnections = new List<GameClientConnection>();
            _removedClientConnections = new List<GameClientConnection>();
            _udpMessagesToHandle = new ThreadSafeWrapper<List<Tuple<Message, IPEndPoint>>>(new List<Tuple<Message, IPEndPoint>>());
            MessageHandlers = new List<IMessageHandler>();
            InitializeUDPSocket();
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

        public IEnumerable<GameClientConnection> GameClientConnections
        {
            get
            {
                if (_gameClientConnections == null) throw new ConnectionException("No connections to game clients");
                return _gameClientConnections;
            }
        }

        public Connection GameServerConnection
        {
            get
            {
                if (_gameServerConnection == null) throw new ConnectionException("No connection to game server");
                return _gameServerConnection;
            }
        }

        public Connection ManagementServerConnection
        {
            get
            {
                if (_managementServerConnection == null) throw new ConnectionException("No connection to management server");
                return _managementServerConnection;
            }
        }

        /// <summary>
        /// UDP socket for use with all remote connections.
        /// </summary>
        public AWUDPSocket UDPSocket { get; private set; }

        /// <summary>
        /// A temporary storage of game client UDP end points. Items come from <see cref="ClientJoinMessage"/> 
        /// and are read and removed by game client connections themselves.
        /// </summary>
        public IList<IPEndPoint[]> ClientUDPEndPointPool { get; private set; }

        /// <summary>
        /// Finds a management server and initialises <see cref="ManagementServerConnection"/>.
        /// May use DNS and take some time to finish.
        /// </summary>
        public void ConnectToManagementServer()
        {
            try
            {
                var managementServerEndPoint = MiscHelper.ParseIPEndPoint(Game.Settings.Net.ManagementServerAddress);
                if (managementServerEndPoint.Port == 0)
                    managementServerEndPoint.Port = MANAGEMENT_SERVER_PORT_DEFAULT;
                _managementServerConnection = new ManagementServerConnection(_game, managementServerEndPoint);
            }
            catch (ArgumentException e)
            {
                throw new ArgumentException("ERROR: Invalid IP address for management server: " + Game.Settings.Net.ManagementServerAddress, e);
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
            ClientUDPEndPointPool = new List<IPEndPoint[]>();
            _startServerConnectionHandler = connectionHandler;
            _connectionAttemptListener = new ConnectionAttemptListener(_game);
            _connectionAttemptListener.StartListening(TCP_CONNECTION_PORT);
            RegisterServerToManagementServer();
            _game.UpdateGameServerInfoToManagementServer();
        }

        /// <summary>
        /// Turns this game server into a standalone game instance and disposes of
        /// any connections to game clients.
        /// </summary>
        public void StopServer()
        {
            Log.Write("Server stops listening");
            MessageHandlers.Clear();
            UnregisterServerFromManagementServer();
            _connectionAttemptListener.StopListening();
            _connectionAttemptListener = null;
            ClientUDPEndPointPool = null;
            DisposeGameClientConnections();
            FlushUnhandledUDPMessages();
        }

        /// <summary>
        /// Turns this game instance into a game client by connecting to a game server.
        /// Poll <c>Connection.ConnectionResults</c> to find out when and if
        /// the connection was successfully estblished.
        /// </summary>
        public void StartClient(AssaultWing game, AWEndPoint[] serverEndPoints, Action<Result<Connection>> connectionHandler)
        {
            Log.Write("Client starts connecting");
            _startClientConnectionHandler = connectionHandler;
            Connection.Connect(game, serverEndPoints);
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
            DisposeGameServerConnection();
            FlushUnhandledUDPMessages();
        }

        /// <summary>
        /// Drops the connection to a game client. To be called only
        /// as the game server.
        /// </summary>
        /// <param name="error">If true, client is being dropped due to an error condition.</param>
        public void DropClient(int connectionID, bool error)
        {
            if (Game.NetworkMode != NetworkMode.Server)
                throw new InvalidOperationException("Cannot drop client in mode " + Game.NetworkMode);

            var connection = GetGameClientConnection(connectionID);
            if (_removedClientConnections.Contains(connection)) return;
            Log.Write("Dropping " + connection.Name);
            connection.ConnectionStatus.IsDropped = true;
            _removedClientConnections.Add(connection);

            // Remove the client's players.
            if (error)
            {
                var droppedPlayerNames =
                    from plr in Game.DataEngine.Spectators
                    where plr.ConnectionID == connection.ID
                    select plr.Name;
                if (droppedPlayerNames.Any())
                {
                    var message = string.Join(" and ", droppedPlayerNames.ToArray()) + " dropped out";
                    foreach (var player in Game.DataEngine.Players.Where(plr => !plr.IsRemote))
                        player.Messages.Add(new PlayerMessage(message, Player.DEFAULT_COLOR));
                }
            }
            Game.DataEngine.Spectators.Remove(player => player.ConnectionID == connection.ID);
        }

        public GameClientConnection GetGameClientConnection(int connectionID)
        {
            return _gameClientConnections.First(conn => conn.ID == connectionID);
        }

        public Connection GetConnection(int connectionID)
        {
            var result = AllConnections.First(conn => conn.ID == connectionID);
            if (result == null) throw new ArgumentException("Connection not found with ID " + connectionID);
            return result;
        }

        /// <summary>
        /// Sends a message to all game clients. Use this method instead of enumerating
        /// over <see cref="GameClientConnections"/> and sending to each separately.
        /// </summary>
        public void SendToGameClients(Message message)
        {
            foreach (var conn in _gameClientConnections) conn.Send(message);
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
                return _gameServerConnection.PingInfo.PingTime;
            }
        }

        /// <summary>
        /// Round-trip ping time to a game client.
        /// </summary>
        public TimeSpan GetClientPingTime(int connectionID)
        {
            return GetGameClientConnection(connectionID).PingInfo.PingTime;
        }

        /// <summary>
        /// Returns the number of frames elapsed since the message was sent.
        /// </summary>
        public int GetMessageAge(GameplayMessage message)
        {
            return GetMessageAge(message, GetConnection(message.ConnectionID));
        }

        /// <summary>
        /// Returns the number of frames elapsed since the message was sent,
        /// given the connection the message was sent on.
        /// </summary>
        public int GetMessageAge(GameplayMessage message, Connection connection)
        {
            if (connection.ID != message.ConnectionID) throw new ArgumentException("Wrong connection");
            var localFrameCountOnReceive = Game.DataEngine.ArenaFrameCount;
            var localFrameCountOnSend = message.FrameNumber + connection.PingInfo.RemoteFrameNumberOffset;
            return localFrameCountOnReceive - localFrameCountOnSend;
        }

        /// <summary>
        /// Returns the total game time at which the message was current.
        /// </summary>
        public TimeSpan GetMessageGameTime(GameplayMessage message)
        {
            return Game.TargetElapsedTime.Multiply(message.FrameNumber);
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

        public override void Update()
        {
            if (_connectionAttemptListener != null && _connectionAttemptListener.IsListening)
                _connectionAttemptListener.Update();
            HandleNewConnections();
            HandleUDPMessages();
            HandleClientState();
            foreach (var conn in AllConnections) conn.UpdatePingInfo();
            foreach (var handler in MessageHandlers.ToList()) // enumerate over a copy to allow adding MessageHandlers during enumeration
                if (!handler.Disposed) handler.HandleMessages();
            RemoveDisposedMessageHandlers();
            HandleErrors();
            RemoveClosedConnections();
            PurgeUnhandledMessages();
        }

        public override void Dispose()
        {
            DisposeGameClientConnections();
            DisposeUDPSocket();
            base.Dispose();
        }

        #endregion GameComponent methods

        #region Private methods

        private IEnumerable<Connection> AllConnections
        {
            get
            {
                if (_managementServerConnection != null)
                    yield return _managementServerConnection;
                if (_gameServerConnection != null)
                    yield return _gameServerConnection;
                if (_gameClientConnections != null)
                    foreach (var conn in _gameClientConnections) yield return conn;
            }
        }

        private void HandleNewConnections()
        {
            Connection.ConnectionResults.Do(queue =>
            {
                while (queue.Count > 0)
                {
                    var result = queue.Dequeue();
                    switch (Game.NetworkMode)
                    {
                        case NetworkMode.Client:
                            if (_gameServerConnection != null) break; // silently ignore extra server connection attempts
                            if (result.Successful) _gameServerConnection = result.Value;
                            _startClientConnectionHandler(result);
                            break;
                        case NetworkMode.Server:
                            // Silently ignore extra server connection attempts.
                            // Note: This will only allow one game client behind any one NAT.
                            if (_gameClientConnections.Any(conn => conn.RemoteIPAddress.Equals(result.Value.RemoteIPAddress))) break;
                            if (result.Successful) _gameClientConnections.Add((GameClientConnection)result.Value);
                            _startServerConnectionHandler(result);
                            break;
                        default:
                            // HACK: This happens when client connects to two server end points and both fail.
                            // The first failure returns to NetworkMode.Standalone and the second failure comes here.
                            Log.Write("Invalid NetworkMode for accepting connections: " + Game.NetworkMode);
                            break;
                    }
                }
            });
        }

        private void HandleErrors()
        {
            foreach (var conn in AllConnections) conn.HandleErrors();
            HandleUDPSocketErrors();
        }

        private void HandleUDPSocketErrors()
        {
            if (UDPSocket == null) return;
            bool errorsFound = false;
            UDPSocket.Errors.Do(queue =>
            {
                while (queue.Count > 0)
                {
                    errorsFound = true;
                    var errorMessage = queue.Dequeue();
                    Log.Write("Error occurred with UDP socket: " + errorMessage);
                }
            });
            if (errorsFound)
            {
                Log.Write("Closing network connections due to errors");
                _game.CutNetworkConnections();
            }
        }

        private void DisposeGameServerConnection()
        {
            if (_gameServerConnection == null) return;
            _gameServerConnection.Dispose();
            _gameServerConnection = null;
        }

        private void DisposeGameClientConnections()
        {
            foreach (var conn in _gameClientConnections) conn.Dispose();
            _gameClientConnections.Clear();
        }

        private void RegisterServerToManagementServer()
        {
            var message = new RegisterGameServerMessage
            {
                GameServerName = Environment.MachineName,
                MaxClients = 16,
                TCPPort = TCP_CONNECTION_PORT,
                LocalEndPoint = new AWEndPoint(UDPSocket.PrivateLocalEndPoint, TCP_CONNECTION_PORT)
            };
            ManagementServerConnection.Send(message);
        }

        private void UnregisterServerFromManagementServer()
        {
            var message = new UnregisterGameServerMessage();
            ManagementServerConnection.Send(message);
        }

        private void InitializeUDPSocket()
        {
            UDPSocket = new AWUDPSocket(HandleUDPMessage);
        }

        private void DisposeUDPSocket()
        {
            if (UDPSocket == null) return;
            UDPSocket.Dispose();
            UDPSocket = null;
        }

        private void FlushUnhandledUDPMessages()
        {
            _udpMessagesToHandle.Do(list => list.Clear());
        }

        private void HandleUDPMessage(ArraySegment<byte> messageHeaderAndBody, IPEndPoint remoteEndPoint)
        {
            var message = remoteEndPoint.Equals(ManagementServerConnection.RemoteUDPEndPoint)
                ? ManagementMessage.Deserialize(messageHeaderAndBody, Game.GameTime.TotalRealTime)
                : Message.Deserialize(messageHeaderAndBody, Game.GameTime.TotalRealTime);
            _udpMessagesToHandle.Do(list => list.Add(Tuple.Create(message, remoteEndPoint)));
        }

        private void HandleUDPMessages()
        {
            _udpMessagesToHandle.Do(messages =>
            {
                foreach (var messageAndEndPoint in messages)
                {
                    if (messageAndEndPoint.Item1 is ManagementMessage)
                    {
                        messageAndEndPoint.Item1.ConnectionID = ManagementServerConnection.ID;
                        ManagementServerConnection.Messages.Do(queue => queue.Enqueue(messageAndEndPoint.Item1));
                    }
                    else
                    {
                        // Messages from unknown sources will be silently ignored.
                        var connection = GetConnection(messageAndEndPoint.Item2);
                        if (connection != null)
                        {
                            messageAndEndPoint.Item1.ConnectionID = connection.ID;
                            connection.HandleMessage(messageAndEndPoint.Item1, messageAndEndPoint.Item2);
                        }
                    }
                }
                messages.Clear();
            });
        }

        /// <summary>
        /// Returns a connection with the given end point, or null if none exists.
        /// </summary>
        private Connection GetConnection(IPEndPoint remoteUDPEndPoint)
        {
            // Compare IP address from TCP end point because UDP end point may still
            // be unknown. IP address should be the same in both end points.
            // Note: This limits us to one game client behind each NAT except the NAT
            // of the game server. This can be fixed by making the management server pass
            // the game client's UDP end point to the game server.
            return AllConnections.FirstOrDefault(conn => conn.RemoteIPAddress.Equals(remoteUDPEndPoint.Address));
        }

        private void HandleClientState()
        {
            if (Game.NetworkMode != NetworkMode.Server) return;
            var serverIsPlayingArena = Game.DataEngine.Arena != null;
            foreach (var conn in GameClientConnections)
            {
                var clientIsPlayingArena = conn.ConnectionStatus.IsRunningArena;
                if (!clientIsPlayingArena && serverIsPlayingArena)
                    MakeClientStartArena(conn);
                else if (clientIsPlayingArena && !serverIsPlayingArena)
                    MakeClientStopArena(conn);
            }
        }

        private void MakeClientStartArena(GameClientConnection conn)
        {
            var arenaName = _game.SelectedArenaName;
            conn.Send(new StartGameMessage { ArenaToPlay = arenaName });
            var gobCreationMessage = new GobCreationMessage();
            foreach (var gob in Game.DataEngine.Arena.Gobs.Where(g => g.IsRelevant))
                gobCreationMessage.AddGob(gob);
            conn.Send(gobCreationMessage);
            conn.ConnectionStatus.CurrentArenaName = arenaName;
        }

        private void MakeClientStopArena(GameClientConnection conn)
        {
            conn.Send(new ArenaFinishMessage());
            conn.ConnectionStatus.CurrentArenaName = null;
        }

        private void ApplicationExitCallback(object caller, EventArgs args)
        {
            Dispose();
        }

        private void RemoveDisposedMessageHandlers()
        {
            MessageHandlers = MessageHandlers.Except(MessageHandlers.Where(handler => handler.Disposed)).ToList();
        }

        private void RemoveClosedConnections()
        {
            foreach (var connection in _removedClientConnections)
            {
                connection.Dispose();
                _gameClientConnections.Remove(connection);
            }
            _removedClientConnections.Clear();
            if (_managementServerConnection != null && _managementServerConnection.IsDisposed)
                _managementServerConnection = null;
            if (_gameServerConnection != null && _gameServerConnection.IsDisposed)
                _gameServerConnection = null;
            _gameClientConnections.RemoveAll(c => c.IsDisposed);
        }

        [System.Diagnostics.Conditional("DEBUG")]
        private void PurgeUnhandledMessages()
        {
            Type lastMessageType = null; // to avoid flooding log messages
            Connection lastConnection = null;
            foreach (var connection in AllConnections)
                connection.Messages.Do(queue => queue.Prune(
                    message => message.CreationTime < Game.GameTime.TotalRealTime - TimeSpan.FromSeconds(30),
                    message =>
                    {
                        if (lastMessageType != message.GetType() || lastConnection != connection)
                        {
                            lastMessageType = message.GetType();
                            lastConnection = connection;
                            Log.Write("WARNING: Purging messages of type " + message.Type + " received from " + connection.Name);
                        }
                    }));
        }

        #endregion Private methods
    }
}
