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
        private List<Connection> _gameClientConnections;

        /// <summary>
        /// Clients to be removed from <c>clientConnections</c>.
        /// </summary>
        private List<Connection> _removedClientConnections;

        /// <summary>
        /// Handler of connection results for client that is connecting to a game server.
        /// </summary>
        private Action<Result<Connection>> _startClientConnectionHandler;

        /// <summary>
        /// Handler of connection results for server that is listening for game client connections.
        /// </summary>
        private Action<Result<Connection>> _startServerConnectionHandler;

        private List<NetBuffer> _udpMessagesToHandle;
        private ConnectionAttemptListener _connectionAttemptListener;

        #endregion Fields

        #region Constructor

        public NetworkEngine(AssaultWingCore game)
            : base(game)
        {
            _gameClientConnections = new List<Connection>();
            _removedClientConnections = new List<Connection>();
            _udpMessagesToHandle = new List<NetBuffer>();
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

        public IEnumerable<Connection> GameClientConnections
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
                _managementServerConnection = new ManagementServerConnection(Game, managementServerEndPoint);
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
            _connectionAttemptListener = new ConnectionAttemptListener(Game);
            _connectionAttemptListener.StartListening(TCP_CONNECTION_PORT);
            RegisterServerToManagementServer();
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
        }

        /// <summary>
        /// Turns this game instance into a game client by connecting to a game server.
        /// Poll <c>Connection.ConnectionResults</c> to find out when and if
        /// the connection was successfully estblished.
        /// </summary>
        public void StartClient(AssaultWingCore game, AWEndPoint[] serverEndPoints, Action<Result<Connection>> connectionHandler)
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
            _removedClientConnections.Add(connection);

            // Remove the client's players.
            if (error)
            {
                List<string> droppedPlayerNames = new List<string>();
                foreach (var player in Game.DataEngine.Spectators)
                    if (player.ConnectionID == connection.ID)
                        droppedPlayerNames.Add(player.Name);
                string message = string.Join(" and ", droppedPlayerNames.ToArray()) + " dropped out";
                foreach (var player in Game.DataEngine.Players)
                    if (!player.IsRemote)
                        player.SendMessage(message);
            }
            Game.DataEngine.Spectators.Remove(player => player.ConnectionID == connection.ID);
        }

        public Connection GetGameClientConnection(int connectionID)
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
        /// Returns the number of bytes waiting to be sent through the network.
        /// </summary>
        public int GetSendQueueSize()
        {
            return AllConnections.Sum(conn => conn.GetSendQueueSize());
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
        /// Offset of game time on the server compared to this game instance.
        /// </summary>
        /// Adding the offset to the server game time gives our game time.
        public TimeSpan ServerGameTimeOffset
        {
            get
            {
                if (_gameServerConnection == null)
                    throw new InvalidOperationException("Cannot count server game time offset without connection");
                return _gameServerConnection.PingInfo.RemoteGameTimeOffset;
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
        /// Offset of game time on a client compared to this server instance.
        /// </summary>
        /// Adding the offset to the client game time gives our game time.
        public TimeSpan GetClientGameTimeOffset(int connectionID)
        {
            return GetGameClientConnection(connectionID).PingInfo.RemoteGameTimeOffset;
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
            // TODO: Assuming the remote and local game instances are frame synchronized,
            // we can do without the parameter 'connection' as 'RemoteFrameNumberOffset' will be zero.
            if (connection.ID != message.ConnectionID) throw new ArgumentException("Wrong connection");
            var messageAge = Game.DataEngine.ArenaFrameCount
                - (message.FrameNumber + connection.PingInfo.RemoteFrameNumberOffset);
            return messageAge;
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

            // Update ping time measurements.
            foreach (var conn in AllConnections) conn.Update();

            foreach (var handler in MessageHandlers.ToList()) // enumerate over a copy to allow adding MessageHandlers during enumeration
                if (!handler.Disposed) handler.HandleMessages();
            RemoveDisposedMessageHandlers();
            HandleErrors();
            RemoveClosedConnections();

#if DEBUG
            // Look for unhandled messages.
            Type lastMessageType = null; // to avoid flooding log messages
            Connection lastConnection = null;
            foreach (var connection in AllConnections)
                connection.Messages.Prune(
                    message => message.CreationTime < Game.GameTime.TotalRealTime - TimeSpan.FromSeconds(30),
                    message =>
                    {
                        if (lastMessageType != message.GetType() || lastConnection != connection)
                        {
                            lastMessageType = message.GetType();
                            lastConnection = connection;
                            Log.Write("WARNING: Purging messages of type " + message.Type + " received from " + connection.Name);
                        }
                    });
#endif
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
                            if (result.Successful) _gameClientConnections.Add(result.Value);
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
                    var e = queue.Dequeue();
                    Log.Write("Error occurred with UDP socket", e);
                }
            });
            if (errorsFound)
            {
                Log.Write("Closing network connections due to errors");
                Game.CutNetworkConnections();
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
            UDPSocket.StartThreads();
        }

        private void DisposeUDPSocket()
        {
            if (UDPSocket == null) return;
            UDPSocket.Dispose();
            UDPSocket = null;
        }

        private void HandleUDPMessage(NetBuffer messageHeaderAndBody)
        {
            lock (_udpMessagesToHandle) _udpMessagesToHandle.Add(messageHeaderAndBody);
        }

        private void HandleUDPMessages()
        {
            lock (_udpMessagesToHandle)
            {
                foreach (var messageHeaderAndBody in _udpMessagesToHandle)
                {
                    if (messageHeaderAndBody.EndPoint.Equals(ManagementServerConnection.RemoteUDPEndPoint))
                    {
                        var message = ManagementMessage.Deserialize(messageHeaderAndBody.Buffer, messageHeaderAndBody.Length, Game.GameTime.TotalRealTime);
                        ManagementServerConnection.Messages.Enqueue(message);
                    }
                    else
                    {
                        var connection = GetConnection(messageHeaderAndBody.EndPoint);
                        if (connection == null)
                        {
#if DEBUG
                            Log.Write("Note: Ignoring an UDP message from unknown source " + messageHeaderAndBody.EndPoint);
#endif
                            break;
                        }
                        connection.HandleMessage(messageHeaderAndBody);
                    }
                }
                _udpMessagesToHandle.Clear();
            }
        }

        /// <summary>
        /// Returns a connection with the given end point, or null if none exists.
        /// </summary>
        private Connection GetConnection(IPEndPoint remoteUDPEndPoint)
        {
            // Compare IP address from TCP end point because UDP end point may still
            // be unknown. IP address should be the same in both end points.
            return AllConnections.FirstOrDefault(conn => conn.RemoteIPAddress.Equals(remoteUDPEndPoint.Address));
        }

        private void TerminateThread(SuspendableThread thread)
        {
            if (thread == null) return;
            thread.Terminate();
            if (!thread.Join(TimeSpan.FromSeconds(1)))
                AW2.Helpers.Log.Write("WARNING: NetworkEngine was unable to kill " + thread.Name);
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
            Connection conn;
            while ((conn = _gameClientConnections.FirstOrDefault(c => c.IsDisposed)) != null)
                _gameClientConnections.Remove(conn);
        }

        #endregion Private methods
    }
}
