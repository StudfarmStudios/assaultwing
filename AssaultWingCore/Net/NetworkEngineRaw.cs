using System.Net;
using System.Net.Sockets;
using AW2.Core;
using AW2.Game;
using AW2.Helpers;
using AW2.Net.Connections;
using AW2.Net.ConnectionUtils;
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
    /// <see cref="NetworkEngine"/> reacts to incoming messages according
    /// to message handlers that other components register.
    /// </para>
    /// <seealso cref="Message.ConnectionID"/>
    public class NetworkEngineRaw : NetworkEngine
    {
        #region Type definitions

        private enum ConnectionType
        {
            GameServer,
            GameClient
        }

        #endregion Type definitions

        #region Fields

        private const int UDP_CONNECTION_PORT_FIRST = 'A' * 256 + 'W';
        private const int UDP_CONNECTION_PORT_LAST = UDP_CONNECTION_PORT_FIRST + 9;
        private const string NETWORK_TRACE_FILE = "AWnetwork.log";
        private static readonly TimeSpan HANDSHAKE_TIMEOUT = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan HANDSHAKE_ATTEMPT_INTERVAL = TimeSpan.FromSeconds(0.9);

        private AssaultWingCore _game;

        /// <summary>
        /// Clients to be removed from <c>clientConnections</c>.
        /// </summary>
        private List<GameClientConnectionRaw> _removedClientConnections;

        /// <summary>
        /// Handler of connection results for client that is connecting to a game server.
        /// </summary>
        private Action<IResult<Connection>> _startClientConnectionHandler;

        private ThreadSafeWrapper<List<Tuple<Message, IPEndPoint>>> _udpMessagesToHandle;
        private ConnectionAttemptListener _connectionAttemptListener;

        #endregion Fields

        #region Constructor

        public NetworkEngineRaw(AssaultWingCore game, int updateOrder)
            : base(game, updateOrder)
        {
            _game = game;
            _GameClientConnections = new List<GameClientConnectionRaw>();
            _removedClientConnections = new List<GameClientConnectionRaw>();
            _udpMessagesToHandle = new ThreadSafeWrapper<List<Tuple<Message, IPEndPoint>>>(new List<Tuple<Message, IPEndPoint>>());
            _MessageHandlers = new List<MessageHandlerBase>();
            InitializeUDPSocket();
        }

        #endregion Constructor

        #region Public interface

        private List<MessageHandlerBase> _MessageHandlers;

        /// <summary>
        /// The handlers of network messages.
        /// </summary>
        override public List<MessageHandlerBase> MessageHandlers { get { return _MessageHandlers;} }

        /// <summary>
        /// Are we connected to a game server.
        /// </summary>
        override public bool IsConnectedToGameServer { get { return _GameServerConnection != null; } }

        private List<GameClientConnectionRaw> _GameClientConnections;

        /// <summary>
        /// Network connections to game clients. Nonempty only on a game server.
        /// </summary>
        override public IEnumerable<GameClientConnection> GameClientConnections { get {return _GameClientConnections;} }

        private GameServerConnectionRaw _GameServerConnection;

        /// <summary>
        /// Network connection to the game server of the current game session, 
        /// or <c>null</c> if no such live connection exists 
        /// (including the case that we are the game server).
        /// </summary>
        override public Connection GameServerConnection { get { return _GameServerConnection; } }

        /// <summary>
        /// UDP socket for use with all remote connections.
        /// </summary>
        public AWUDPSocket UDPSocket { get; private set; }

        override public byte[] GetAssaultWingInstanceKey()
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
        /// Turns this game instance into a game server to whom other game instances
        /// can connect as game clients.
        /// </summary>
        /// <param name="connectionHandler">Handler of connection result.</param>
        override public void StartServer(Func<bool> allowNewConnection)
        {
            Log.Write("Starting game server");
            AllowNewServerConnection = allowNewConnection;
            _connectionAttemptListener = new ConnectionAttemptListener(_game);
            _connectionAttemptListener.StartListening(Game.Settings.Net.GameServerPort, UDPSocket.PrivateLocalEndPoint.Port);
        }

        /// <summary>
        /// Turns this game server into a standalone game instance and disposes of
        /// any connections to game clients.
        /// </summary>
        override public void StopServer()
        {
            Log.Write("Stopping game server");
            MessageHandlers.Clear();
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
        override public void StartClient(AssaultWingCore game, AWEndPoint[] serverEndPoints, Action<IResult<Connection>> connectionHandler)
        {
            Log.Write("Client starts connecting");
            _startClientConnectionHandler = connectionHandler;
            ConnectionRaw.Connect(game, serverEndPoints);
        }

        /// <summary>
        /// Turns this game client into a standalone game instance by disconnecting
        /// from the game server.
        /// </summary>
        override public void StopClient()
        {
            Log.Write("Client closes connection");
            MessageHandlers.Clear();
            ConnectionRaw.CancelConnect();
            DisposeGameServerConnection();
            FlushUnhandledUDPMessages();
        }

        /// <summary>
        /// Drops the connection to a game client. To be called only as the game server.
        /// </summary>
        override public void DropClient(int connectionID)
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

        /// <summary>
        /// Send dummy UDP packets to probable UDP end points of the client to increase
        /// probability of our NAT forwarding UDP packets from the client to us.
        /// </summary>
        override public void DoClientUdpHandshake(GameServerHandshakeRequestTCP mess) {
            var ping = new PingRequestMessage();
            for (int port = NetworkEngineRaw.UDP_CONNECTION_PORT_FIRST; port <= NetworkEngineRaw.UDP_CONNECTION_PORT_LAST; port++)
                UDPSocket.Send(ping.Serialize, new IPEndPoint(GetConnection(mess.ConnectionID).RemoteTCPEndPoint.Address, port));            
        }

        override public GameClientConnectionRaw GetGameClientConnection(int connectionID)
        {
            return _GameClientConnections.First(conn => conn.ID == connectionID);
        }

        override public ConnectionRaw GetConnection(int connectionID)
        {
            var result = AllConnections.First(conn => conn.ID == connectionID);
            if (result == null) throw new ArgumentException("Connection not found with ID " + connectionID);
            return result;
        }

        override public string GetConnectionAddressString(int connectionID)
        {
            var result = AllConnections.First(conn => conn.ID == connectionID);
            if (result == null) return $"Unknown connection {connectionID}";
            return result.RemoteTCPEndPoint.Address.ToString();
        }

        /// <summary>
        /// Sends a message to all game clients. Use this method instead of enumerating
        /// over <see cref="GameClientConnections"/> and sending to each separately.
        /// </summary>
        override public void SendToGameClients(Message message)
        {
            foreach (var conn in GameClientConnections) conn.Send(message);
        }

        /// <summary>
        /// Round-trip ping time to the game server.
        /// </summary>
        override public TimeSpan ServerPingTime
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

        override protected IEnumerable<ConnectionRaw> AllConnections
        {
            get
            {
                if (IsConnectedToGameServer)
                    yield return _GameServerConnection;
                if (_GameClientConnections != null)
                    foreach (var conn in _GameClientConnections) yield return conn;
            }
        }

        private Func<bool> AllowNewServerConnection;

        private void HandleIncomingServerConnection(IResult<Connection> result)
        {
            var conn = (GameClientConnectionRaw)result.Value;
            if (!result.Successful)
                Log.Write("Some client failed to connect: " + result.Error);
            else if (AllowNewServerConnection())
            {
                _GameClientConnections.Add(conn);
                Log.Write("Server obtained {0} from {1}", conn.Name, conn.RemoteTCPEndPoint);
            }
            else
            {
                var mess = new ConnectionClosingMessage { Info = "game server refused joining" };
                result.Value.Send(mess);
                Log.Write("Server refused connection from " + conn.RemoteTCPEndPoint);
            }
        }

        private void HandleConnectionResultOnClient(Result<ConnectionRaw> result) {
            var conn = (GameServerConnectionRaw)result.Value;

            if (IsConnectedToGameServer)
            {
                // Silently ignore extra server connection attempts.
                if (result.Successful) conn.Dispose();
                return;
            }

            if (!result.Successful)
            {
                Log.Write("Failed to connect to server: " + result.Error);
            }
            else
            {
                _GameServerConnection = conn;
            }

            _startClientConnectionHandler(result);
        }


        private void HandleNewConnections()
        {
            ConnectionRaw.ConnectionResults.Do(queue =>
            {
                while (queue.Count > 0)
                {
                    var result = queue.Dequeue();
                    switch (Game.NetworkMode)
                    {
                        case NetworkMode.Client:
                            HandleConnectionResultOnClient(result);
                            break;
                        case NetworkMode.Server:
                            HandleIncomingServerConnection(result);
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
            _GameServerConnection.Dispose();
            _GameServerConnection = null;
        }

        private void DisposeGameClientConnections()
        {
            foreach (var conn in _GameClientConnections) conn.Dispose();
            _GameClientConnections.Clear();
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
            var message = Message.Deserialize(buffer, Game.GameTime.TotalRealTime);
            if (Game.NetworkMode == NetworkMode.Server && message is GameServerHandshakeRequestUDP)
                HandleGameServerHandshakeRequestUDP((GameServerHandshakeRequestUDP)message, remoteEndPoint);
            else
                _udpMessagesToHandle.Do(list => list.Add(Tuple.Create(message, remoteEndPoint)));
            return buffer.Count;
        }

        private void HandleGameServerHandshakeRequestUDP(GameServerHandshakeRequestUDP mess, IPEndPoint remoteEndPoint)
        {
            foreach (var conn in _GameClientConnections)
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
                    // Messages from unknown sources will be silently ignored.
                    var connection = GetConnection(messageAndEndPoint.Item2);
                    if (connection != null)
                    {
                        messageAndEndPoint.Item1.ConnectionID = connection.ID;
                        connection.HandleMessage(messageAndEndPoint.Item1);
                    }
                }
                messages.Clear();
            });
        }

        /// <summary>
        /// Returns a connection with the given end point, or null if none exists.
        /// </summary>
        private ConnectionRaw GetConnection(IPEndPoint remoteUDPEndPoint)
        {
            return AllConnections.FirstOrDefault(conn =>
                conn.RemoteUDPEndPoint != null && (conn.RemoteUDPEndPoint.Equals(remoteUDPEndPoint) ||
                (conn.RemoteUDPEndPointAlternative != null && conn.RemoteUDPEndPointAlternative.Equals(remoteUDPEndPoint))));
        }

        private void ApplicationExitCallback(object caller, EventArgs args)
        {
            Dispose();
        }

        private void RemoveDisposedMessageHandlers()
        {
            _MessageHandlers = _MessageHandlers.Except(MessageHandlers.Where(handler => handler.Disposed)).ToList();
        }

        private void HandleConnectionHandshakingOnServer()
        {
            foreach (var conn in _GameClientConnections)
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
            if (_GameServerConnection.PreviousHandshakeAttempt == TimeSpan.Zero)
                _GameServerConnection.FirstHandshakeAttempt = _GameServerConnection.PreviousHandshakeAttempt = Game.GameTime.TotalRealTime;
            if (Game.GameTime.TotalRealTime > _GameServerConnection.FirstHandshakeAttempt + HANDSHAKE_TIMEOUT) return;
            if (Game.GameTime.TotalRealTime > _GameServerConnection.PreviousHandshakeAttempt + HANDSHAKE_ATTEMPT_INTERVAL)
            {
                _GameServerConnection.PreviousHandshakeAttempt = Game.GameTime.TotalRealTime;
                var handshake = new GameServerHandshakeRequestUDP { GameClientKey = GetAssaultWingInstanceKey() };
                GameServerConnection.Send(handshake);
            }
        }

        private void RemoveClosedConnections()
        {
            foreach (var connection in _removedClientConnections)
            {
                connection.Dispose();
                _GameClientConnections.Remove(connection);
            }
            _removedClientConnections.Clear();
            if (_GameServerConnection != null && _GameServerConnection.IsDisposed)
                _GameServerConnection = null;
            if (Game.DataEngine.Arena != null)
            {
                foreach (var conn in _GameClientConnections)
                    if (conn.IsDisposed)
                        foreach (var gob in Game.DataEngine.Arena.GobsInRelevantLayers)
                            gob.ClientStatus[1 << conn.ID] = false;
            }
            _GameClientConnections.RemoveAll(c => c.IsDisposed);
        }


        #endregion Private methods
    }
}
