using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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

        public const int UDP_CONNECTION_PORT_FIRST = 'A' * 256 + 'W';
        public const int UDP_CONNECTION_PORT_LAST = UDP_CONNECTION_PORT_FIRST + 9;
        private const int MANAGEMENT_SERVER_PORT_DEFAULT = 'A' * 256 + 'W';
        private const string NETWORK_TRACE_FILE = "AWnetwork.log";
        private static readonly TimeSpan HANDSHAKE_TIMEOUT = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan HANDSHAKE_ATTEMPT_INTERVAL = TimeSpan.FromSeconds(0.9);

        private AssaultWing _game;

        /// <summary>
        /// Network connection to the management server, 
        /// or <c>null</c> if no such live connection exists.
        /// </summary>
        private ManagementServerConnection _managementServerConnection;
        private bool _managementServerConnectionLost;
        private AWTimer _managementServerConnectionCheckTimer;

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
            GameClientConnections = new List<GameClientConnection>();
            _removedClientConnections = new List<GameClientConnection>();
            _udpMessagesToHandle = new ThreadSafeWrapper<List<Tuple<Message, IPEndPoint>>>(new List<Tuple<Message, IPEndPoint>>());
            _managementServerConnectionCheckTimer = new AWTimer(() => _game.GameTime.TotalRealTime, TimeSpan.FromSeconds(10)) { SkipPastIntervals = true };
            MessageHandlers = new List<MessageHandlerBase>();
            InitializeUDPSocket();
        }

        #endregion Constructor

        #region Public interface

        /// <summary>
        /// The handlers of network messages.
        /// </summary>
        public List<MessageHandlerBase> MessageHandlers { get; private set; }

        /// <summary>
        /// Are we connected to a game server.
        /// </summary>
        public bool IsConnectedToGameServer { get { return GameServerConnection != null; } }

        /// <summary>
        /// Are we connected to the management server.
        /// </summary>
        public bool IsConnectedToManagementServer { get { return _managementServerConnection != null; } }

        /// <summary>
        /// Network connections to game clients. Nonempty only on a game server.
        /// </summary>
        public List<GameClientConnection> GameClientConnections { get; private set; }

        /// <summary>
        /// Network connection to the game server of the current game session, 
        /// or <c>null</c> if no such live connection exists 
        /// (including the case that we are the game server).
        /// </summary>
        public Connection GameServerConnection { get; set; }

        public ManagementServerConnection ManagementServerConnection
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

        public byte[] GetAssaultWingInstanceKey()
        {
            // Mix MAC address with local UDP port to get a unique ID across different computers
            // and across different Assault Wing instances running on the same computer.
            var port = UDPSocket.PrivateLocalEndPoint.Port;
            var clientKey = new byte[UDPSocket.MACAddress.Length + 2];
            clientKey[0] = (byte)port;
            clientKey[1] = (byte)(port >> 8);
            Array.Copy(UDPSocket.MACAddress, 0, clientKey, 2, UDPSocket.MACAddress.Length);
            return clientKey;
        }

        /// <summary>
        /// Does nothing if a connection to the management server already exists.
        /// Otherwise, finds a management server and initialises <see cref="ManagementServerConnection"/>.
        /// May use DNS and take some time to finish. May throw <see cref="ArgumentException"/>.
        /// </summary>
        public void EnsureConnectionToManagementServer()
        {
            if (_managementServerConnection != null) return;
            var managementServerEndPoint = MiscHelper.ParseIPEndPoint(Game.Settings.Net.ManagementServerAddress);
            if (managementServerEndPoint.Port == 0)
                managementServerEndPoint.Port = MANAGEMENT_SERVER_PORT_DEFAULT;
            _managementServerConnection = new ManagementServerConnection(_game, managementServerEndPoint);
        }

        /// <summary>
        /// Turns this game instance into a game server to whom other game instances
        /// can connect as game clients.
        /// </summary>
        /// <param name="connectionHandler">Handler of connection result.</param>
        public void StartServer(Action<Result<Connection>> connectionHandler)
        {
            Log.Write("Starting game server");
            _startServerConnectionHandler = connectionHandler;
            _connectionAttemptListener = new ConnectionAttemptListener(_game);
            _connectionAttemptListener.StartListening(Game.Settings.Net.GameServerPort, UDPSocket.PrivateLocalEndPoint.Port);
            RegisterServerToManagementServer();
        }

        /// <summary>
        /// Turns this game server into a standalone game instance and disposes of
        /// any connections to game clients.
        /// </summary>
        public void StopServer()
        {
            Log.Write("Stopping game server");
            MessageHandlers.Clear();
            UnregisterServerFromManagementServer();
            _connectionAttemptListener.StopListening();
            _connectionAttemptListener = null;
            var shutdownNotice = new ConnectionClosingMessage { Info = "server shut down" };
            foreach (var conn in GameClientConnections) conn.Send(shutdownNotice);
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
        /// Drops the connection to a game client. To be called only as the game server.
        /// </summary>
        public void DropClient(int connectionID)
        {
            if (Game.NetworkMode != NetworkMode.Server)
                throw new InvalidOperationException("Cannot drop client in mode " + Game.NetworkMode);

            var connection = GetGameClientConnection(connectionID);
            if (_removedClientConnections.Contains(connection)) return;
            Log.Write("Dropping " + connection.Name);
            connection.ConnectionStatus.State = GameClientStatus.StateType.Dropped;
            _removedClientConnections.Add(connection);

            var droppedPlayers = Game.DataEngine.Spectators.Where(plr => plr.ConnectionID == connection.ID);
            foreach (var plr in droppedPlayers) plr.Disconnect();
            if (droppedPlayers.Any())
            {
                var message = string.Join(" and ", droppedPlayers.Select(plr => plr.Name).ToArray()) + " left the game";
                foreach (var player in Game.DataEngine.Players)
                    player.Messages.Add(new PlayerMessage(message, PlayerMessage.DEFAULT_COLOR));
            }
        }

        public GameClientConnection GetGameClientConnection(int connectionID)
        {
            return GameClientConnections.First(conn => conn.ID == connectionID);
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
            foreach (var conn in GameClientConnections) conn.Send(message);
        }

        /// <summary>
        /// Round-trip ping time to the game server.
        /// </summary>
        public TimeSpan ServerPingTime
        {
            get
            {
                if (!IsConnectedToGameServer)
                    throw new InvalidOperationException("Cannot ping server without connection");
                return GameServerConnection.PingInfo.PingTime;
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
            var localFrameCountOnSend = message.FrameNumber + connection.PingInfo.PingTime.Divide(2).Frames();
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
            foreach (var conn in AllConnections) conn.Update();
            foreach (var handler in MessageHandlers.ToList()) // enumerate over a copy to allow adding MessageHandlers during enumeration
                if (!handler.Disposed) handler.HandleMessages();
            RemoveDisposedMessageHandlers();
            switch (Game.NetworkMode)
            {
                case NetworkMode.Server:
                    CheckManagementServerConnection();
                    HandleConnectionHandshakingOnServer();
                    break;
                case NetworkMode.Client:
                    HandleConnectionHandshakingOnClient();
                    break;
            }
            DetectSilentConnections();
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
                if (IsConnectedToManagementServer)
                    yield return _managementServerConnection;
                if (IsConnectedToGameServer)
                    yield return GameServerConnection;
                if (GameClientConnections != null)
                    foreach (var conn in GameClientConnections) yield return conn;
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
                            _startClientConnectionHandler(result);
                            break;
                        case NetworkMode.Server:
                            _startServerConnectionHandler(result);
                            break;
                        default:
                            // This happens when client connects to two server end points and both fail.
                            // The first failure reverts the client into a standalone game instance,
                            // and the second failure ends up here.
                            break;
                    }
                }
            });
        }

        private void DetectSilentConnections()
        {
            foreach (var conn in AllConnections)
                if (conn.PingInfo.IsMissingReplies)
                    conn.Errors.Do(queue => queue.Enqueue("Ping replies missing."));
        }

        private void HandleErrors()
        {
            foreach (var conn in AllConnections) conn.HandleErrors();
            HandleUDPSocketErrors();
        }

        private void HandleUDPSocketErrors()
        {
            if (UDPSocket == null) return;
            UDPSocket.Errors.Do(queue =>
            {
                while (queue.Count > 0)
                {
                    var errorMessage = queue.Dequeue();
                    Log.Write("Error occurred with UDP socket: " + errorMessage);
                }
            });
        }

        private void DisposeGameServerConnection()
        {
            if (!IsConnectedToGameServer) return;
            GameServerConnection.Dispose();
            GameServerConnection = null;
        }

        private void DisposeGameClientConnections()
        {
            foreach (var conn in GameClientConnections) conn.Dispose();
            GameClientConnections.Clear();
        }

        private void RegisterServerToManagementServer()
        {
            var message = new RegisterGameServerMessage
            {
                GameServerName = Game.Settings.Net.GameServerName,
                MaxClients = Game.Settings.Net.GameServerMaxPlayers,
                TCPPort = Game.Settings.Net.GameServerPort,
                LocalEndPoint = new AWEndPoint(UDPSocket.PrivateLocalEndPoint, Game.Settings.Net.GameServerPort),
                AWVersion = MiscHelper.Version,
            };
            ManagementServerConnection.Send(message);
            _game.UpdateGameServerInfoToManagementServer();
        }

        private void UnregisterServerFromManagementServer()
        {
            var message = new UnregisterGameServerMessage();
            ManagementServerConnection.Send(message);
        }

        private void InitializeUDPSocket()
        {
            for (int udpPort = UDP_CONNECTION_PORT_FIRST; UDPSocket == null && udpPort <= UDP_CONNECTION_PORT_LAST; udpPort++)
                try
                {
                    UDPSocket = new AWUDPSocket(udpPort, HandleUDPMessage);
                }
                catch (SocketException)
                {
                }
            if (UDPSocket != null)
                Log.Write("Using UDP port " + UDPSocket.PrivateLocalEndPoint.Port);
            else
                Log.Write("Failed to obtain UDP port. Cannot play on the network.");
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

        private int HandleUDPMessage(ArraySegment<byte> buffer, IPEndPoint remoteEndPoint)
        {
            var message = IsConnectedToManagementServer && remoteEndPoint.Equals(ManagementServerConnection.RemoteUDPEndPoint)
                ? ManagementMessage.Deserialize(buffer, Game.GameTime.TotalRealTime)
                : Message.Deserialize(buffer, Game.GameTime.TotalRealTime);
            if (Game.NetworkMode == NetworkMode.Server && message is GameServerHandshakeRequestUDP)
                HandleGameServerHandshakeRequestUDP((GameServerHandshakeRequestUDP)message, remoteEndPoint);
            else
                _udpMessagesToHandle.Do(list => list.Add(Tuple.Create(message, remoteEndPoint)));
            return buffer.Count;
        }

        private void HandleGameServerHandshakeRequestUDP(GameServerHandshakeRequestUDP mess, IPEndPoint remoteEndPoint)
        {
            foreach (var conn in GameClientConnections)
                if (conn.ConnectionStatus.ClientKey != null && conn.ConnectionStatus.ClientKey.SequenceEqual(mess.GameClientKey))
                    if (conn.RemoteUDPEndPoint == null)
                        conn.RemoteUDPEndPoint = remoteEndPoint;
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
            return AllConnections.FirstOrDefault(conn =>
                conn.RemoteUDPEndPoint != null && (conn.RemoteUDPEndPoint.Equals(remoteUDPEndPoint) ||
                (conn.RemoteUDPEndPointAlternative != null && conn.RemoteUDPEndPointAlternative.Equals(remoteUDPEndPoint))));
        }

        private void HandleClientState()
        {
            if (Game.NetworkMode != NetworkMode.Server) return;
            var serverIsPlayingArena = Game.DataEngine.Arena != null && Game.LogicEngine.Enabled;
            foreach (var conn in GameClientConnections)
            {
                var clientIsPlayingArena = conn.ConnectionStatus.IsRunningArena;
                // Note: Some gobs refer to players, so start arena not until player info has been sent.
                if (!clientIsPlayingArena && serverIsPlayingArena && conn.ConnectionStatus.HasPlayerSettings)
                    MakeClientStartArena(conn);
                else if (clientIsPlayingArena && !serverIsPlayingArena)
                    MakeClientStopArena(conn);
            }
        }

        private void MakeClientStartArena(GameClientConnection conn)
        {
            var arenaName = _game.SelectedArenaName;
            var startGameMessage = new StartGameMessage
            {
                ArenaID = Game.DataEngine.Arena.ID,
                ArenaToPlay = arenaName,
                ArenaTimeLeft = Game.DataEngine.ArenaFinishTime == TimeSpan.Zero ? TimeSpan.Zero : Game.DataEngine.ArenaFinishTime - Game.GameTime.TotalRealTime,
                WallCount = Game.DataEngine.Arena.Gobs.OfType<AW2.Game.Gobs.Wall>().Count()
            };
            conn.Send(startGameMessage);
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

        private void CheckManagementServerConnection()
        {
            if (!_managementServerConnectionCheckTimer.IsElapsed) return;
            var managementServerConnectionLostEarlier = _managementServerConnectionLost;
            _managementServerConnectionLost = !ManagementServerConnection.HasReceivedPingsRecently;
            if (!_managementServerConnectionLost) return;
            if (!managementServerConnectionLostEarlier)
                Log.Write("Connection to management server lost. Clients may not be able to join. Reconnecting...");
            RegisterServerToManagementServer();
        }

        private void HandleConnectionHandshakingOnServer()
        {
            foreach (var conn in GameClientConnections)
            {
                if (conn.FirstHandshakeAttempt == TimeSpan.Zero)
                    conn.FirstHandshakeAttempt = Game.GameTime.TotalRealTime;
                if (!conn.IsHandshaken && Game.GameTime.TotalRealTime > conn.FirstHandshakeAttempt + HANDSHAKE_TIMEOUT)
                {
                    conn.Send(new ConnectionClosingMessage { Info = "handshake failed" });
                    DropClient(conn.ID);
                }
            }
        }

        private void HandleConnectionHandshakingOnClient()
        {
            if (!IsConnectedToGameServer) return;
            if (GameServerConnection.PreviousHandshakeAttempt == TimeSpan.Zero)
                GameServerConnection.FirstHandshakeAttempt = GameServerConnection.PreviousHandshakeAttempt = Game.GameTime.TotalRealTime;
            if (Game.GameTime.TotalRealTime > GameServerConnection.FirstHandshakeAttempt + HANDSHAKE_TIMEOUT) return;
            if (Game.GameTime.TotalRealTime > GameServerConnection.PreviousHandshakeAttempt + HANDSHAKE_ATTEMPT_INTERVAL)
            {
                GameServerConnection.PreviousHandshakeAttempt = Game.GameTime.TotalRealTime;
                var handshake = new GameServerHandshakeRequestUDP { GameClientKey = GetAssaultWingInstanceKey() };
                GameServerConnection.Send(handshake);
            }
        }

        private void RemoveClosedConnections()
        {
            foreach (var connection in _removedClientConnections)
            {
                connection.Dispose();
                GameClientConnections.Remove(connection);
            }
            _removedClientConnections.Clear();
            if (_managementServerConnection != null && _managementServerConnection.IsDisposed)
                _managementServerConnection = null;
            if (GameServerConnection != null && GameServerConnection.IsDisposed)
                GameServerConnection = null;
            if (Game.DataEngine.Arena != null)
            {
                foreach (var conn in GameClientConnections)
                    if (conn.IsDisposed)
                        foreach (var gob in Game.DataEngine.Arena.GobsInRelevantLayers)
                            gob.ClientStatus[1 << conn.ID] = false;
            }
            GameClientConnections.RemoveAll(c => c.IsDisposed);
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
