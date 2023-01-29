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

namespace AW2.Net.Connections
{
    /// <summary>
    /// A connection to a remote host over a network. Communication between 
    /// the local and remote host is done by messages. 
    /// </summary>
    /// <remarks>
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
    /// </remarks>
    public abstract class ConnectionRaw : ConnectionBase
    {
        #region Fields

        private static List<ConnectAsyncState> g_connectAsyncStates = new List<ConnectAsyncState>();

        /// <summary>
        /// TCP socket to the connected remote host.
        /// </summary>
        private AWTCPSocket _tcpSocket;
        // FIXME !!! The socket may dispose itself in a background thread at any time e.g. in
        // AWTCPSocket.ReceiveCompleted. Suggestion: all disposing should happen in the main thread.

        private IPEndPoint _remoteUDPEndPoint;
        private object _lock = new object();

        #endregion Fields

        #region Properties

        /// <summary>
        /// Has the connection handshake been completed. Sending UDP messages is not possible before the
        /// handshaking is complete.
        /// </summary>
        public bool IsHandshaken { get { return RemoteUDPEndPoint != null; } }

        /// <summary>
        /// In real time seconds.
        /// </summary>
        public TimeSpan FirstHandshakeAttempt { get; set; }
        public TimeSpan PreviousHandshakeAttempt { get; set; }

        /// <summary>
        /// The remote TCP end point of the connection.
        /// </summary>
        /// <see cref="System.Net.Sockets.Socket.RemoteEndPoint"/>
        public IPEndPoint RemoteTCPEndPoint
        {
            get
            {
                if (IsDisposed) throw new InvalidOperationException("This connection has been disposed");
                try
                {
                    return _tcpSocket.RemoteEndPoint;
                }
                catch (Exception e)
                {
                    Errors.Do(queue => queue.Enqueue("Error during RemoteTCPEndPoint: " + e));
                }
                return new IPEndPoint(0, 0); // FIXME
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
        /// Null or an alternative remote UDP end point where messages may come from. This is a workaround for routers
        /// that show their public UDP port in connections they established (e.g. game server to management server)
        /// but show their private UDP port for connections that the remote host established (e.g. game server
        /// to game client when a TCP connection has already been established).
        /// </summary>
        public IPEndPoint RemoteUDPEndPointAlternative { get; set; }

        /// <summary>
        /// Results of connection attempts.
        /// </summary>
        public static ThreadSafeWrapper<Queue<Result<ConnectionRaw>>> ConnectionResults { get; private set; }

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

        #endregion Properties

        #region Public interface

        /// <summary>
        /// Starts opening a connection to a remote host.
        /// </summary>
        /// <param name="remoteEndPoints">Alternative end points to connect to.</param>
        public static void Connect(AssaultWingCore game, AWEndPointRaw[] remoteEndPoints)
        {
            var sockets = new Socket[remoteEndPoints.Length];
            var asyncState = new ConnectAsyncState(game, sockets, remoteEndPoints);
            lock (g_connectAsyncStates) g_connectAsyncStates.Add(asyncState);
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
            lock (g_connectAsyncStates) foreach (var state in g_connectAsyncStates) state.Cancel();
        }

        /// <summary>
        /// Call this every frame.
        /// </summary>
        public virtual void Update()
        {
            if (IsHandshaken) PingInfo.Update();
            _tcpSocket.FlushSendBuffer();
        }

        /// <summary>
        /// Sends a message to the remote host. The message is sent asynchronously,
        /// so there is no guarantee when the transmission will be finished.
        /// </summary>
        override public void Send(Message message)
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
            }
            catch (SocketException e)
            {
                Errors.Do(queue => queue.Enqueue("SocketException in Send: " + e.SocketErrorCode));
            }
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
            errorsFound |= HandleErrorQueue(_tcpSocket.Errors, "Error occurred with TCP socket of ");
            errorsFound |= HandleErrorQueue(Errors, "Error occurred with ");
            if (errorsFound)
            {
                Log.Write("Closing " + Name + " due to errors");
                Dispose(true);
            }
        }

        private bool HandleErrorQueue(ThreadSafeWrapper<Queue<string>> errors, string errorMessagePrefix)
        {
            var errorsFound = false;
            errors.Do(queue =>
            {
                while (queue.Count > 0)
                {
                    errorsFound = true;
                    var e = queue.Dequeue();
                    Log.Write(errorMessagePrefix + Name + ": " + e);
                }
            });
            return errorsFound;
        }

        private int HandleMessageBuffer(ArraySegment<byte> buffer, IPEndPoint remoteEndPoint)
        {
            if (buffer.Count >= Message.HEADER_LENGTH && !Message.IsValidHeader(buffer))
            {
                Errors.Do(queue => queue.Enqueue("Connection received an invalid message header [" +
                    MiscHelper.BytesToString(new ArraySegment<byte>(buffer.Array, 0, Message.HEADER_LENGTH)) + "]"));
                return buffer.Count;
            }
            var messageLength = Message.HEADER_LENGTH + Message.GetBodyLength(buffer);
            if (buffer.Count >= messageLength)
            {
                var message = Message.Deserialize(buffer, Game.GameTime.TotalRealTime);
                message.ConnectionID = ID;
                HandleMessage(message);
                return messageLength;
            }
            return 0;
        }

        public static void HandleNewConnection(Result<ConnectionRaw> result)
        {
            if (result == null) return;
            ReportResult(result);
        }

        #endregion Public interface

        #region Non-public methods

        static ConnectionRaw()
        {
            ConnectionResults = new ThreadSafeWrapper<Queue<Result<ConnectionRaw>>>(new Queue<Result<ConnectionRaw>>());
        }

        override public void QueueError(string message)
        {
            Errors.Do(queue => queue.Enqueue(message));
        }

        /// <summary>
        /// Performs the actual disposing.
        /// </summary>
        /// <param name="error">If <c>true</c> then an internal error has occurred.</param>
        override protected void DisposeImpl(bool error)
        {
            _tcpSocket.Dispose();
            DisposeId();
        }

        /// <summary>
        /// Creates a new connection to a remote host.
        /// </summary>
        /// <param name="tcpSocket">An opened TCP socket to the remote host. The
        /// created connection owns the socket and will dispose of it.</param>
        /// The client program can create <see cref="Connection">Connections</see>
        /// via the static methods <see cref="StartListening(int, string)"/> and 
        /// <see cref="Connect(IPAddress, int, string)"/>.
        protected ConnectionRaw(AssaultWingCore game, Socket tcpSocket)
            : base(game)
        {
            if (tcpSocket == null) throw new ArgumentNullException("tcpSocket", "Null socket argument");
            if (!tcpSocket.Connected) throw new ArgumentException("Socket not connected", "tcpSocket");
            _tcpSocket = new AWTCPSocket(tcpSocket, HandleMessageBuffer);
        }

        /// <summary>
        /// Sends raw byte data to the remote host via TCP. The data is sent asynchronously,
        /// so there is no guarantee when the transmission will be finished.
        /// </summary>
        private void SendViaTCP(Action<NetworkBinaryWriter> writeData)
        {
            if (_tcpSocket == null) throw new InvalidOperationException("Connection has no TCP socket for sending a message");
            _tcpSocket.AddToSendBuffer(writeData);
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
            ((NetworkEngineRaw)Game.NetworkEngine).UDPSocket.Send(writeData, RemoteUDPEndPoint);
        }

        public static void ReportResult(Result<ConnectionRaw> result)
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
            lock (g_connectAsyncStates) g_connectAsyncStates.Remove((ConnectAsyncState)asyncResult.AsyncState);
            var result = ConnectAsyncState.ConnectionAttemptCallback(asyncResult, () => CreateServerConnection(asyncResult, index));
            HandleNewConnection(result);
        }

        private static ConnectionRaw CreateServerConnection(IAsyncResult asyncResult, int index)
        {
            var state = (ConnectAsyncState)asyncResult.AsyncState;
            var socket = state.Sockets[index];
            var remoteEndPoint = state.RemoteEndPoints[index];
            try
            {
                socket.EndConnect(asyncResult);
                var connection = new GameServerConnectionRaw(state.Game, socket);
                connection.RemoteUDPEndPoint = remoteEndPoint.UDPEndPoint;
                // On 2012-03-04 there were always two remote end points. If this changes, upgrade RemoteUDPPortAlternative to List<int>.
                if (state.RemoteEndPoints.Length == 2)
                    connection.RemoteUDPEndPointAlternative = new IPEndPoint(connection.RemoteUDPEndPoint.Address, state.RemoteEndPoints[1 - index].UDPEndPoint.Port);
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
