//#define DEBUG_SENT_BYTE_COUNT // dumps to log an itemised count of sent bytes every second
//#define DEBUG_MESSAGE_DELAY // delays message sending to simulate lag
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;
using AW2.Helpers;
using AW2.Helpers.Collections;

namespace AW2.Net
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
    public class Connection : IConnection
    {
        #region Fields

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
        private Socket _socket;

        /// <summary>
        /// Buffer of serialised messages waiting to be sent to the remote host.
        /// </summary>
        private ThreadSafeWrapper<Queue<ArraySegment<byte>>> _sendBuffers;

        /// <summary>
        /// The thread that is continuously reading incoming data from the remote host.
        /// </summary>
        private SuspendableThread _readThread;

        /// <summary>
        /// The thread that is continuously sending outgoing data to the remote host.
        /// </summary>
        private SuspendableThread _sendThread;

        /// <summary>
        /// Received messages that are waiting for consumption by the client program.
        /// </summary>
        private TypedQueue<Message> _messages;

        /// <summary>
        /// Information on general error situations.
        /// </summary>
        private ThreadSafeWrapper<Queue<Exception>> _errors;

#if DEBUG_SENT_BYTE_COUNT
        private static TimeSpan _lastPrintTime = new TimeSpan(-1);
        private static Dictionary<Type, int> _messageSizes = new Dictionary<Type, int>();
#endif

#if DEBUG_MESSAGE_DELAY
        // TimeSpan is the time to send the message
        private Queue<AW2.Helpers.Pair<Message, TimeSpan>> _messagesToSend = new Queue<AW2.Helpers.Pair<Message, TimeSpan>>();
#endif

        #endregion Fields

        #region Properties

        /// <summary>
        /// Unique identifier of the connection. Nonnegative.
        /// </summary>
        public int Id { get; private set; }

        /// <summary>
        /// Short, human-readable name of the connection.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The local end point of the connection.
        /// </summary>
        /// <see cref="System.Net.Sockets.Socket.LocalEndPoint"/>
        public IPEndPoint LocalEndPoint
        {
            get
            {
                try
                {
                    return (IPEndPoint)_socket.LocalEndPoint;
                }
                catch (Exception e)
                {
                    _errors.Do(queue => queue.Enqueue(e));
                }
                return new IPEndPoint(IPAddress.None, 0);
            }
        }

        /// <summary>
        /// The remote end point of the connection.
        /// </summary>
        /// <see cref="System.Net.Sockets.Socket.RemoteEndPoint"/>
        public IPEndPoint RemoteEndPoint
        {
            get
            {
                try
                {
                    return (IPEndPoint)_socket.RemoteEndPoint;
                }
                catch (Exception e)
                {
                    _errors.Do(queue => queue.Enqueue(e));
                }
                return new IPEndPoint(IPAddress.None, 0);
            }
        }

        /// <summary>
        /// Received messages that are waiting for consumption by the client program.
        /// </summary>
        public ITypedQueue<Message> Messages { get { return _messages; } }

        /// <summary>
        /// Called after a new element has been added to <c>Messages</c>.
        /// </summary>
        public event Action MessageCallback;

        /// <summary>
        /// Results of connection attempts.
        /// </summary>
        public static ThreadSafeWrapper<Queue<Result<Connection>>> ConnectionResults { get; private set; }

        /// <summary>
        /// Information about general error situations.
        /// </summary>
        public ThreadSafeWrapper<Queue<Exception>> Errors { get { return _errors; } }

        /// <summary>
        /// Called after a new element has been added to <c>Errors</c>.
        /// </summary>
        public event Action ErrorCallback;

        #endregion Properties

        #region Public interface

        /// <summary>
        /// Starts opening a connection to a remote host at an address and port.
        /// </summary>
        /// <param name="address">Address of the remote host.</param>
        /// <param name="port">Listening port of the remote host.</param>
        public static void Connect(IPAddress address, int port)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var asyncState = new ConnectAsyncState(socket);
            g_connectAsyncStates.Add(asyncState);
            socket.BeginConnect(address, port, ConnectCallback, asyncState);
        }

        /// <summary>
        /// Cancels all current connection attempts to a remote host.
        /// </summary>
        public static void CancelConnect()
        {
            foreach (var state in g_connectAsyncStates) state.Cancel();
        }

        /// <summary>
        /// Updates the connection. Call this regularly.
        /// </summary>
        public void Update() { }

        /// <summary>
        /// Sends a message to the remote host. The message is sent asynchronously,
        /// so there is no guarantee when the transmission will be finished.
        /// </summary>
        /// <param name="message">The message to send.</param>
        public void Send(Message message)
        {
            try
            {
#if DEBUG_MESSAGE_DELAY
                // Store message and check if there are old messages to send.
                TimeSpan sendTime = AssaultWing.Instance.GameTime.TotalRealTime + TimeSpan.FromMilliseconds(100);
                messagesToSend.Enqueue(new AW2.Helpers.Pair<Message, TimeSpan>(message, sendTime));
                while (messagesToSend.Peek().Second <= AssaultWing.Instance.GameTime.TotalRealTime)
                {
                    var sendMessage = messagesToSend.Dequeue().First;
                    Send(sendMessage.Serialize());
                }
#else
                byte[] data = message.Serialize();
                Send(data);
#endif
#if DEBUG_SENT_BYTE_COUNT
                if (_lastPrintTime + TimeSpan.FromSeconds(1) < AssaultWing.Instance.GameTime.TotalRealTime)
                {
                    _lastPrintTime = AssaultWing.Instance.GameTime.TotalRealTime;
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
                Errors.Do(queue => queue.Enqueue(e));
            }

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
            bool errorsFound = false;
            Errors.Do(queue =>
            {
                while (queue.Count > 0)
                {
                    errorsFound = true;
                    Exception e = queue.Dequeue();
                    AW2.Helpers.Log.Write("Error occurred with " + Name + ": " + e.Message);
                }
            });
            if (errorsFound)
            {
                AW2.Helpers.Log.Write("Closing " + Name + " due to errors");
                Dispose(true);
            }
        }

        /// <summary>
        /// Returns the number of bytes waiting to be sent through this connection.
        /// </summary>
        public int GetSendQueueSize()
        {
            int count = 0;
            _sendBuffers.Do(queue =>
            {
                foreach (ArraySegment<byte> segment in queue)
                    count += segment.Count;
            });
            return count;
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
        /// <param name="error">If <c>true</c> then an internal error
        /// has occurred.</param>
        protected void Dispose(bool error)
        {
            if (Interlocked.Exchange(ref _isDisposed, 1) > 0) return;
            DisposeImpl(error);
        }

        /// <summary>
        /// Performs the actual diposing.
        /// </summary>
        /// <param name="error">If <c>true</c> then an internal error
        /// has occurred.</param>
        /// <seealso cref="Dispose()"/>
        protected virtual void DisposeImpl(bool error)
        {
            Application.ApplicationExit -= ApplicationExitCallback;

            if (_readThread != null)
            {
                _readThread.Terminate();
                if (!_readThread.Join(TimeSpan.FromSeconds(1)))
                    AW2.Helpers.Log.Write("WARNING: Unable to kill read loop of " + Name);
                _readThread = null;
            }
            if (_sendThread != null)
            {
                _sendThread.Terminate();
                if (!_sendThread.Join(TimeSpan.FromSeconds(1)))
                    AW2.Helpers.Log.Write("WARNING: Unable to kill write loop of " + Name);
                _sendThread = null;
            }
            if (!error)
                _socket.Shutdown(SocketShutdown.Both);
            _socket.Close();
        }

        /// <summary>
        /// Creates a new connection to a remote host.
        /// </summary>
        /// <param name="socket">An opened socket to the remote host. The
        /// created connection owns the socket and will dispose of it.</param>
        /// The client program can create <see cref="Connection">Connections</see>
        /// via the static methods <see cref="StartListening(int, string)"/> and 
        /// <see cref="Connect(IPAddress, int, string)"/>.
        protected Connection(Socket socket)
        {
            if (socket == null) throw new ArgumentNullException("Null socket argument");
            if (!socket.Connected) throw new ArgumentException("Socket not connected");
            Id = g_leastUnusedID++;
            Name = "Connection " + Id;
            Application.ApplicationExit += ApplicationExitCallback;
            ConfigureSocket(socket);
            _socket = socket;
            _messages = new TypedQueue<Message>();
            _sendBuffers = new ThreadSafeWrapper<Queue<ArraySegment<byte>>>(new Queue<ArraySegment<byte>>());
            _errors = new ThreadSafeWrapper<Queue<Exception>>(new Queue<Exception>());
            _readThread = new MessageReadThread(socket, ThreadExceptionHandler, MessageHandler);
            _readThread.Start();
            _sendThread = new MessageSendThread(socket, _sendBuffers, ThreadExceptionHandler);
            _sendThread.Start();
        }

        /// <summary>
        /// Sends raw byte data to the remote host. The data is sent asynchronously,
        /// so there is no guarantee when the transmission will be finished.
        /// </summary>
        /// <param name="data">The data to send.</param>
        private void Send(byte[] data)
        {
            _sendBuffers.Do(queue => queue.Enqueue(new ArraySegment<byte>(data)));
        }

        private void ApplicationExitCallback(object caller, EventArgs args)
        {
            Dispose(false);
        }

        private void ThreadExceptionHandler(Exception e)
        {
            _errors.Do(queue =>
            {
                queue.Enqueue(e);
                if (ErrorCallback != null) ErrorCallback();
            });
        }

        private void MessageHandler(byte[] messageHeaderBuffer, byte[] messageBodyBuffer)
        {
            var message = Message.Deserialize(messageHeaderBuffer, messageBodyBuffer, Id);
            _messages.Enqueue(message);
            lock (_messages) if (MessageCallback != null) MessageCallback();
        }

        private static void ConfigureSocket(Socket socket)
        {
            // TODO: Remove this log output from public release!!!
            Log.Write("Configuring socket:\n  " + string.Join("\n  ", GetSocketInfoStrings(socket).ToArray()));
            socket.Blocking = true;
            socket.ReceiveTimeout = 0; // don't time out on receiving
            socket.SendTimeout = 1000;
            socket.NoDelay = true;
            // TODO: Remove this log output from public release!!!
            Log.Write("...configured to:\n  " + string.Join("\n  ", GetSocketInfoStrings(socket).ToArray()));
        }

        private static IEnumerable<string> GetSocketInfoStrings(Socket socket)
        {
            return
                from p in typeof(Socket).GetProperties()
                let s = GetProperty(p, socket)
                where s != null
                orderby s ascending
                select s;
        }

        private static string GetProperty(System.Reflection.PropertyInfo prop, Socket socket)
        {
            try
            {
                if ((prop.Name == "EnableBroadcast" && socket.ProtocolType != ProtocolType.Udp) ||
                    (prop.Name == "MulticastLoopback" && socket.ProtocolType == ProtocolType.Tcp))
                    return null;
                return prop.Name + ": " + prop.GetValue(socket, null).ToString();
            }
            catch (System.Reflection.TargetInvocationException e)
            {
                Log.Write("Error reading Socket property " + prop.Name + ": " + e);
            };
            return null;
        }

        #endregion Non-public methods

        #region Private callback implementations

        /// <summary>
        /// Callback implementation for finishing an outgoing connection attempt.
        /// </summary>
        /// <param name="asyncResult">The result of the asynchronous operation.</param>
        private static void ConnectCallback(IAsyncResult asyncResult)
        {
            g_connectAsyncStates.Remove((ConnectAsyncState)asyncResult.AsyncState);
            ConnectAsyncState.ConnectionAttemptCallback(asyncResult, () => CreateServerConnection(asyncResult), ReportResult);
        }

        private static Connection CreateServerConnection(IAsyncResult asyncResult)
        {
            var state = (ConnectAsyncState)asyncResult.AsyncState;
            state.Socket.EndConnect(asyncResult);
            return new GameServerConnection(state.Socket);
        }
        
        private static void ReportResult(Result<Connection> result)
        {
            ConnectionResults.Do(queue => queue.Enqueue(result));
        }

        #endregion Private callback implementations
    }
}
