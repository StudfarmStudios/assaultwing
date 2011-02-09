//#define DEBUG_SENT_BYTE_COUNT // dumps to log an itemised count of sent bytes every second
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using AW2.Core;
using AW2.Helpers;
using AW2.Helpers.Collections;
using AW2.Helpers.Serialization;
using AW2.Net.ConnectionUtils;
using AW2.Net.Messages;

namespace AW2.Net.Connections
{
    /// <summary>
    /// A connection to a remote host over a network. Communication between 
    /// the local and remote host is done by messages. 
    /// </summary>
    /// Connection operates asynchronously. Both creation of connections and 
    /// sending messages via connections are done asynchronously. Therefore 
    /// their result is not known by the time the corresponding method call returns. 
    /// When results of such actions eventually arrive (as either success or 
    /// failure), they are added to corresponding queues.
    /// It is up to the client program to read the results from the queues.
    /// This can be done handily in the client program main loop. If such 
    /// a loop is not available, or for other reasons, the client program 
    /// can hook up events that notify of finished asynchronous operations.
    /// Such queues exist for connection attempts (static), received messages 
    /// (for each connection) and general error conditions (for each connection).
    /// 
    /// This class is thread safe.
    public abstract class Connection
    {
        #region Fields

        private static readonly TimeSpan SIMULATED_NETWORK_LAG = TimeSpan.FromSeconds(0.0);

        /// <summary>
        /// A meta-value for <see cref="ID"/> denoting an invalid value.
        /// </summary>
        public const int INVALID_ID = -1;

        /// <summary>
        /// Least int that is known not to have been used as a connection identifier.
        /// </summary>
        /// <see cref="Connection.Id"/>
        private static int g_leastUnusedID = 0;

        private static List<ConnectAsyncState> g_connectAsyncStates = new List<ConnectAsyncState>();

        /// <summary>
        /// If greater than zero, then the connection is disposed and thus no longer usable.
        /// </summary>
        private int _isDisposed;

        /// <summary>
        /// TCP socket to the connected remote host.
        /// </summary>
        private AWTCPSocket _tcpSocket;

        private IPEndPoint _remoteUDPEndPoint;
        private object _lock = new object();

#if DEBUG_SENT_BYTE_COUNT
        private static TimeSpan _lastPrintTime = new TimeSpan(-1);
        private static Dictionary<Type, int> _messageSizes = new Dictionary<Type, int>();
#endif

        #endregion Fields

        #region Properties

        public AssaultWing Game { get; private set; }

        /// <summary>
        /// Unique identifier of the connection. Nonnegative.
        /// </summary>
        public int ID { get; private set; }

        /// <summary>
        /// Short, human-readable name of the connection.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Has the connection handshake been completed. Sending UDP messages is not possible before the
        /// handshaking is complete.
        /// </summary>
        public bool IsHandshaken
        {
            get
            {
                // TODO: If RemoteUDPEndPoint stays null for a long time (5 seconds), cut the connection.
                // TODO: Also, TryInitializeRemoteUDPEndPoint() need not be called but at relaxed intervals (each second).
                if (RemoteUDPEndPoint == null) TryInitializeRemoteUDPEndPoint();
                return RemoteUDPEndPoint != null;
            }
        }

        public bool IsDisposed { get { return _isDisposed > 0; } }

        /// <summary>
        /// The remote TCP end point of the connection.
        /// </summary>
        /// <see cref="System.Net.Sockets.Socket.RemoteEndPoint"/>
        public IPEndPoint RemoteTCPEndPoint
        {
            get
            {
                if (IsDisposed) throw new InvalidOperationException("This connection has been disposed");
                return _tcpSocket.RemoteEndPoint;
            }
        }

        /// <summary>
        /// The remote UDP end point of the connection.
        /// </summary>
        /// <see cref="System.Net.Sockets.Socket.RemoteEndPoint"/>
        public IPEndPoint RemoteUDPEndPoint
        {
            get
            {
                if (IsDisposed) throw new InvalidOperationException("This connection has been disposed");
                return _remoteUDPEndPoint;
            }
            set { _remoteUDPEndPoint = value; }
        }

        /// <summary>
        /// The remote IP address of the connection.
        /// </summary>
        public IPAddress RemoteIPAddress
        {
            get
            {
                if (IsDisposed) throw new InvalidOperationException("This connection has been disposed");
                return _remoteUDPEndPoint != null ? _remoteUDPEndPoint.Address : _tcpSocket.RemoteEndPoint.Address;
            }
        }

        /// <summary>
        /// Received messages that are waiting for consumption by the client program.
        /// </summary>
        public ThreadSafeWrapper<ITypedQueue<Message>> Messages { get; private set; }

        /// <summary>
        /// Results of connection attempts.
        /// </summary>
        public static ThreadSafeWrapper<Queue<Result<Connection>>> ConnectionResults { get; private set; }

        /// <summary>
        /// Information about general error situations in background threads.
        /// </summary>
        public ThreadSafeWrapper<Queue<string>> Errors
        {
            get
            {
                if (_tcpSocket == null) return null;
                return _tcpSocket.Errors;
            }
        }

        public PingInfo PingInfo { get; private set; }

        #endregion Properties

        #region Public interface

        /// <summary>
        /// Starts opening a connection to a remote host.
        /// </summary>
        /// <param name="remoteEndPoints">Alternative end points to connect to.</param>
        public static void Connect(AssaultWing game, AWEndPoint[] remoteEndPoints)
        {
            var sockets = new Socket[remoteEndPoints.Length];
            var asyncState = new ConnectAsyncState(game, sockets, remoteEndPoints);
            g_connectAsyncStates.Add(asyncState);
            for (int i = 0; i < remoteEndPoints.Length; i++)
            {
                int index = i;
                sockets[i] = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                sockets[i].BeginConnect(remoteEndPoints[i].TCPEndPoint, result => ConnectCallback(result, index), asyncState);
            }
        }

        /// <summary>
        /// Cancels all current connection attempts to a remote host.
        /// </summary>
        public static void CancelConnect()
        {
            foreach (var state in g_connectAsyncStates) state.Cancel();
        }

        /// <summary>
        /// Updates the connection's ping information. Call this every frame.
        /// </summary>
        public virtual void UpdatePingInfo()
        {
            if (IsHandshaken) PingInfo.Update();
        }

        /// <summary>
        /// Sends a message to the remote host. The message is sent asynchronously,
        /// so there is no guarantee when the transmission will be finished.
        /// </summary>
        /// <param name="message">The message to send.</param>
        public virtual void Send(Message message)
        {
            if (IsDisposed) return;
            try
            {
                switch (message.SendType)
                {
                    case MessageSendType.TCP: SendViaTCP(message.Serialize); break;
                    case MessageSendType.UDP: SendViaUDP(message.Serialize); break;
                    default: throw new MessageException("Unknown send type " + message.SendType);
                }
#if DEBUG_SENT_BYTE_COUNT
                if (_lastPrintTime + TimeSpan.FromSeconds(1) < Game.GameTime.TotalRealTime)
                {
                    _lastPrintTime = Game.GameTime.TotalRealTime;
                    AW2.Helpers.Log.Write("------ SENT_BYTE_COUNT dump");
                    foreach (var pair in _messageSizes)
                        AW2.Helpers.Log.Write(pair.Key.Name + ": " + pair.Value + " bytes");
                    AW2.Helpers.Log.Write("Total " + _messageSizes.Sum(pair => pair.Value) + " bytes");
                    _messageSizes.Clear();
                }
                if (!_messageSizes.ContainsKey(message.GetType()))
                    _messageSizes.Add(message.GetType(), data.Length);
                else
                    _messageSizes[message.GetType()] += data.Length;
#endif
            }
            catch (SocketException e)
            {
                Errors.Do(queue => queue.Enqueue("SocketException in Send: " + e.SocketErrorCode));
            }

        }

        public T TryDequeueMessage<T>() where T : Message
        {
            T value = default(T);
            Messages.Do(queue => value = queue.TryDequeue<T>(m => m.CreationTime <= Game.GameTime.TotalRealTime - SIMULATED_NETWORK_LAG));
            return value;
        }

        /// <summary>
        /// Closes the connection and frees resources it has allocated.
        /// </summary>
        public void Dispose()
        {
            Dispose(false);
        }

        /// <summary>
        /// Reacts to errors that may have occurred during the connection's
        /// operation in background threads.
        /// </summary>
        public void HandleErrors()
        {
            if (IsDisposed) return;
            if (Errors == null) return;
            bool errorsFound = false;
            Errors.Do(queue =>
            {
                while (queue.Count > 0)
                {
                    errorsFound = true;
                    var e = queue.Dequeue();
                    AW2.Helpers.Log.Write("Error occurred with " + Name + ": " + e);
                }
            });
            if (errorsFound)
            {
                AW2.Helpers.Log.Write("Closing " + Name + " due to errors");
                Dispose(true);
            }
        }

        private void HandleMessageBuffer(ArraySegment<byte> messageHeaderAndBody, IPEndPoint remoteEndPoint)
        {
            var message = Message.Deserialize(messageHeaderAndBody, Game.GameTime.TotalRealTime);
            message.ConnectionID = ID;
            HandleMessage(message, remoteEndPoint);
        }

        public void HandleMessage(Message message, IPEndPoint remoteEndPoint)
        {
            if (!TryHandleMessageInternally(message, remoteEndPoint))
                Messages.Do(queue => queue.Enqueue(message));
        }

        public static void HandleNewConnection(Result<Connection> result)
        {
            if (result == null) return;
            ReportResult(result);
        }

        #endregion Public interface

        #region Non-public methods

        static Connection()
        {
            ConnectionResults = new ThreadSafeWrapper<Queue<Result<Connection>>>(new Queue<Result<Connection>>());
        }

        /// <summary>
        /// Closes the connection and frees resources it has allocated.
        /// </summary>
        /// <param name="error">If <c>true</c> then an internal error has occurred.</param>
        protected void Dispose(bool error)
        {
            if (Interlocked.Exchange(ref _isDisposed, 1) > 0) return;
            DisposeImpl(error);
        }

        /// <summary>
        /// Performs the actual disposing.
        /// </summary>
        /// <param name="error">If <c>true</c> then an internal error has occurred.</param>
        protected virtual void DisposeImpl(bool error)
        {
            _tcpSocket.Dispose();
        }

        /// <summary>
        /// Creates a new connection to a remote host.
        /// </summary>
        /// <param name="tcpSocket">An opened TCP socket to the remote host. The
        /// created connection owns the socket and will dispose of it.</param>
        /// The client program can create <see cref="Connection">Connections</see>
        /// via the static methods <see cref="StartListening(int, string)"/> and 
        /// <see cref="Connect(IPAddress, int, string)"/>.
        protected Connection(AssaultWing game, Socket tcpSocket)
            : this(game)
        {
            if (tcpSocket == null) throw new ArgumentNullException("tcpSocket", "Null socket argument");
            if (!tcpSocket.Connected) throw new ArgumentException("Socket not connected", "tcpSocket");
            _tcpSocket = new AWTCPSocket(tcpSocket, HandleMessageBuffer);
        }

        /// <summary>
        /// Creates a UDP-only connection.
        /// </summary>
        protected Connection(AssaultWing game)
        {
            Game = game;
            ID = g_leastUnusedID++;
            Name = "Connection " + ID;
            Messages = new ThreadSafeWrapper<ITypedQueue<Message>>(new TypedQueue<Message>());
            PingInfo = new PingInfo(this);
        }

        /// <summary>
        /// Sends raw byte data to the remote host via TCP. The data is sent asynchronously,
        /// so there is no guarantee when the transmission will be finished.
        /// </summary>
        private void SendViaTCP(Action<NetworkBinaryWriter> writeData)
        {
            if (_tcpSocket == null) throw new InvalidOperationException("Connection has no TCP socket for sending a message");
            _tcpSocket.Send(writeData);
        }

        /// <summary>
        /// Sends raw byte data to the remote host via UDP. The data is sent asynchronously,
        /// so there is no guarantee when the transmission will be finished.
        /// </summary>
        private void SendViaUDP(Action<NetworkBinaryWriter> writeData)
        {
            // Cannot send messages via UDP before the connection is handshaked. But hey,
            // UDP is an unreliable protocol, so let's just dump the message silently in that case.
            if (!IsHandshaken) return;
            Game.NetworkEngine.UDPSocket.Send(writeData, RemoteUDPEndPoint);
        }

        /// <summary>
        /// Returns true if message was interpreted internally by the <see cref="Connection"/>
        /// and needs not be added to the public message queue.
        /// </summary>
        private bool TryHandleMessageInternally(Message mess, IPEndPoint remoteEndPoint)
        {
            // HACK: All we want is, on the game server, to read the sender's (which is a game client)
            // UDP end point from the message. We happen to know that PingRequestMessage is sent via UDP.
            lock (_lock)
            {
                if (!IsHandshaken && mess is PingRequestMessage)
                    RemoteUDPEndPoint = remoteEndPoint;
                return false;
            }
        }

        /// <summary>
        /// Try to find the remote UDP end point from received <see cref="ClientJoinMessage"/>s.
        /// Relevant only on a game server for game client connections.
        /// </summary>
        private void TryInitializeRemoteUDPEndPoint()
        {
            if (Game.NetworkMode != NetworkMode.Server) return;
            if (RemoteUDPEndPoint != null) throw new InvalidOperationException("RemoteUDPEndPoint already initialized");
            var matchingEndPoints = Game.NetworkEngine.ClientUDPEndPointPool
                .FirstOrDefault(endPoints => endPoints.Any(ep => ep.Address.Equals(RemoteIPAddress)));
            if (matchingEndPoints == null) return;
            RemoteUDPEndPoint = matchingEndPoints.First(ep => ep.Address.Equals(RemoteIPAddress));
            Game.NetworkEngine.ClientUDPEndPointPool.Remove(matchingEndPoints);
            // TODO: Remove old items from Game.NetworkEngine.ClientUDPEndPointPool
        }

        public static void ReportResult(Result<Connection> result)
        {
            ConnectionResults.Do(queue => queue.Enqueue(result));
        }

        #endregion Non-public methods

        #region Private callback implementations

        /// <summary>
        /// Callback implementation for finishing an outgoing connection attempt.
        /// </summary>
        /// <param name="asyncResult">The result of the asynchronous operation.</param>
        /// <param name="index">Index of the connection attempt in <c>asyncResult.AsyncState</c></param>
        private static void ConnectCallback(IAsyncResult asyncResult, int index)
        {
            g_connectAsyncStates.Remove((ConnectAsyncState)asyncResult.AsyncState);
            var result = ConnectAsyncState.ConnectionAttemptCallback(asyncResult, () => CreateServerConnection(asyncResult, index));
            HandleNewConnection(result);
        }

        private static Connection CreateServerConnection(IAsyncResult asyncResult, int index)
        {
            var state = (ConnectAsyncState)asyncResult.AsyncState;
            var socket = state.Sockets[index];
            var remoteEndPoint = state.RemoteEndPoints[index];
            try
            {
                socket.EndConnect(asyncResult);
                var connection = new GameServerConnection(state.Game, socket, remoteEndPoint.UDPEndPoint);
                connection.RemoteUDPEndPoint = remoteEndPoint.UDPEndPoint;
                return connection;
            }
            catch (Exception)
            {
#if DEBUG
                Log.Write("Closing server connection socket {0} of {1} due to error (this may be expected)", index + 1, state.Sockets.Length);
#endif
                socket.Close();
                throw;
            }
        }

        #endregion Private callback implementations
    }
}
