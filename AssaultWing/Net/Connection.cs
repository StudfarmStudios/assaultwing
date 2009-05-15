//#define DEBUG_SENT_BYTE_COUNT // dumps to log an itemised count of sent bytes every second
//#define DEBUG_MESSAGE_DELAY // delays message sending to simulate lag
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;

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
    public class Connection
    {
        #region Type definitions

        /// <summary>
        /// State information for an asynchronous connection attempt.
        /// </summary>
        class ConnectAsyncState
        {
            public Socket socket;
            public string id;
            public ConnectAsyncState(Socket socket, string id)
            {
                this.socket = socket;
                this.id = id;
            }
        }

        #endregion Type definitions

        #region Fields

        /// <summary>
        /// Least int that is known not to have been used as a connection identifier.
        /// </summary>
        /// <see cref="Connection.Id"/>
        static int leastUnusedId = 0;

        /// <summary>
        /// If greater than zero, then the connection disposed and thus no longer usable.
        /// </summary>
        int isDisposed;

        /// <summary>
        /// TCP socket to the connected remote host.
        /// </summary>
        Socket socket;

        /// <summary>
        /// Server socket for listening to incoming connection attempts.
        /// <c>null</c> if not in use.
        /// </summary>
        static Socket serverSocket;

        /// <summary>
        /// Buffer for receiving a message header. Has exactly the correct length.
        /// </summary>
        byte[] headerReceiveBuffer;

        /// <summary>
        /// Buffer for receiving a message body. Has sufficient length, but perhaps more.
        /// </summary>
        byte[] bodyReceiveBuffer;

        /// <summary>
        /// Buffer of serialised messages waiting to be sent to the remote host.
        /// </summary>
        ThreadSafeWrapper<Queue<ArraySegment<byte>>> sendBuffers;

        /// <summary>
        /// The thread that is continuously reading incoming data from the remote host.
        /// </summary>
        Thread readThread;

        /// <summary>
        /// The thread that is continuously sending outgoing data to the remote host.
        /// </summary>
        Thread sendThread;

        /// <summary>
        /// Received messages that are waiting for consumption by the client program.
        /// </summary>
        TypedQueue<Message> messages;

        /// <summary>
        /// Results of connection attempts.
        /// </summary>
        static ThreadSafeWrapper<Queue<Result<Connection>>> connectionResults = new ThreadSafeWrapper<Queue<Result<Connection>>>(new Queue<Result<Connection>>());

        /// <summary>
        /// Information on general error situations.
        /// </summary>
        ThreadSafeWrapper<Queue<Exception>> errors;

#if DEBUG_SENT_BYTE_COUNT
        static TimeSpan lastPrintTime = new TimeSpan(-1);
        static Dictionary<Type, int> messageSizes = new Dictionary<Type, int>();
#endif

#if DEBUG_MESSAGE_DELAY
        // TimeSpan is the time to send the message
        Queue<AW2.Helpers.Pair<Message, TimeSpan>> messagesToSend = new Queue<AW2.Helpers.Pair<Message, TimeSpan>>();
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
                    return (IPEndPoint)socket.LocalEndPoint;
                }
                catch (Exception e)
                {
                    errors.Do(queue => queue.Enqueue(e));
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
                    return (IPEndPoint)socket.RemoteEndPoint;
                }
                catch (Exception e)
                {
                    errors.Do(queue => queue.Enqueue(e));
                }
                return new IPEndPoint(IPAddress.None, 0);
            }
        }

        /// <summary>
        /// Received messages that are waiting for consumption by the client program.
        /// </summary>
        public TypedQueue<Message> Messages { get { return messages; } }

        /// <summary>
        /// Called after a new element has been added to <c>Messages</c>.
        /// </summary>
        public event Action MessageCallback;

        /// <summary>
        /// The port at which we are listening for connection attempts from remote hosts.
        /// </summary>
        public static int ListeningPort
        {
            get
            {
                if (!IsListening) throw new InvalidOperationException("Not listening for connections, therefore no listening port exists");
                lock (connectionResults) return ((IPEndPoint)serverSocket.LocalEndPoint).Port;
            }
        }

        /// <summary>
        /// Are we listening for connection attempts from remote hosts.
        /// </summary>
        public static bool IsListening { get { lock (connectionResults) return serverSocket != null; } }

        /// <summary>
        /// Results of connection attempts.
        /// </summary>
        public static ThreadSafeWrapper<Queue<Result<Connection>>> ConnectionResults { get { return connectionResults; } }

        /// <summary>
        /// Called after a new element has been added to <c>ConnectionResults</c>.
        /// </summary>
        public static event Action ConnectionResultCallback;

        /// <summary>
        /// Information about general error situations.
        /// </summary>
        public ThreadSafeWrapper<Queue<Exception>> Errors { get { return errors; } }

        /// <summary>
        /// Called after a new element has been added to <c>Errors</c>.
        /// </summary>
        public event Action ErrorCallback;

        #endregion Properties

        #region Public interface

        /// <summary>
        /// Starts listening for connection attempts from remote hosts.
        /// </summary>
        /// <param name="port">The port at which to listen for incoming connections.</param>
        /// <param name="id">Identifier for distinguishing incoming connection attempts from others.</param>
        public static void StartListening(int port, string id)
        {
            lock (connectionResults)
            {
                if (serverSocket != null)
                    throw new InvalidOperationException("Already listening for incoming connections");
                try
                {
                    serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Any, port);
                    serverSocket.Bind(serverEndPoint);
                    serverSocket.Listen(64);
                    serverSocket.BeginAccept(AcceptCallback, new ConnectAsyncState(serverSocket, id));
                }
                catch (SocketException e)
                {
                    connectionResults.Do(queue =>
                    {
                        queue.Enqueue(new Result<Connection>(e, id));
                        if (ConnectionResultCallback != null) ConnectionResultCallback();
                    });
                }
            }
        }

        /// <summary>
        /// Stops listening for connection attempts from remote hosts.
        /// </summary>
        public static void StopListening()
        {
            lock (connectionResults)
            {
                if (serverSocket == null)
                    throw new Exception("Already not listening for incoming connections");
                serverSocket.Close();
                serverSocket = null;
            }
        }

        /// <summary>
        /// Starts opening a connection to a remote host at an address and port.
        /// </summary>
        /// <param name="address">Address of the remote host.</param>
        /// <param name="port">Listening port of the remote host.</param>
        /// <param name="id">Identifier for distinguishing this connection attempt from others.</param>
        public static void Connect(IPAddress address, int port, string id)
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.BeginConnect(address, port, ConnectCallback, new ConnectAsyncState(socket, id));
        }

        /// <summary>
        /// Sends a message to the remote host. The message is sent asynchronously,
        /// so there is no guarantee when the transmission will be finished.
        /// </summary>
        /// <param name="message">The message to send.</param>
        public void Send(Message message)
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
            if (lastPrintTime + TimeSpan.FromSeconds(1) < AssaultWing.Instance.GameTime.TotalRealTime)
            {
                lastPrintTime = AssaultWing.Instance.GameTime.TotalRealTime;
                AW2.Helpers.Log.Write("------ SENT_BYTE_COUNT dump");
                foreach (var pair in messageSizes)
                    AW2.Helpers.Log.Write(pair.Key.Name + ": " + pair.Value + " bytes");
                messageSizes.Clear();
            }
            if (!messageSizes.ContainsKey(message.GetType()))
                messageSizes.Add(message.GetType(), data.Length);
            else
                messageSizes[message.GetType()] += data.Length;
#endif
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
            sendBuffers.Do(queue =>
            {
                foreach (ArraySegment<byte> segment in queue)
                    count += segment.Count;
            });
            return count;
        }

        #endregion Public interface

        #region Non-public methods

        /// <summary>
        /// Closes the connection and frees resources it has allocated.
        /// </summary>
        /// <param name="error">If <c>true</c> then an internal error
        /// has occurred.</param>
        protected void Dispose(bool error)
        {
            if (Interlocked.Exchange(ref isDisposed, 1) > 0) return;
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

            if (readThread != null && readThread.IsAlive)
            {
                readThread.Abort();
                if (!readThread.Join(1000))
                    AW2.Helpers.Log.Write("WARNING: Unable to kill read loop of " + Name);
                readThread = null;
            }
            if (sendThread != null && sendThread.IsAlive)
            {
                sendThread.Abort();
                if (!sendThread.Join(1000))
                    AW2.Helpers.Log.Write("WARNING: Unable to kill write loop of " + Name);
                sendThread = null;
            }
            if (!error)
                socket.Shutdown(SocketShutdown.Both);
            socket.Close();
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
            if (socket == null)
                throw new ArgumentNullException("Null socket argument");
            if (!socket.Connected)
                throw new ArgumentException("Socket not connected");
            Id = leastUnusedId++;
            Name = "Connection " + Id;
            Application.ApplicationExit += ApplicationExitCallback;
            socket.Blocking = true;
            socket.ReceiveTimeout = 0; // don't time out on receiving
            socket.SendTimeout = 1000;
            this.socket = socket;
            headerReceiveBuffer = new byte[Message.HeaderLength];
            messages = new TypedQueue<Message>();
            sendBuffers = new ThreadSafeWrapper<Queue<ArraySegment<byte>>>(new Queue<ArraySegment<byte>>());
            errors = new ThreadSafeWrapper<Queue<Exception>>(new Queue<Exception>());
            readThread = new Thread(ReadLoop);
            readThread.Name = "Read Loop";
            readThread.Start();
            sendThread = new Thread(SendLoop);
            sendThread.Name = "Send Loop";
            sendThread.Start();
        }

        /// <summary>
        /// Sends raw byte data to the remote host. The data is sent asynchronously,
        /// so there is no guarantee when the transmission will be finished.
        /// </summary>
        /// <param name="data">The data to send.</param>
        void Send(byte[] data)
        {
            sendBuffers.Do(queue => queue.Enqueue(new ArraySegment<byte>(data)));
        }

        /// <summary>
        /// Receives a certain number of bytes to a buffer.
        /// This method blocks until the required number of bytes have been received,
        /// or until the socket is closed or there is some error condition.
        /// </summary>
        /// <param name="buffer">The buffer to store the bytes in.</param>
        /// <param name="byteCount">The number of bytes to receive.</param>
        void Receive(byte[] buffer, int byteCount)
        {
            if (buffer == null) throw new ArgumentNullException("Cannot receive to null buffer");
            if (byteCount < 0) throw new ArgumentException("Cannot receive negative number of bytes");
            int totalReadBytes = 0;
            while (totalReadBytes < byteCount)
            {
                if (socket.Available == 0)
                {
                    // See if the socket is still connected. If Poll() shows that there
                    // is data to read but Available is still zero, the socket must have
                    // been closed at the remote host.
                    if (socket.Poll(100, SelectMode.SelectRead) && socket.Available == 0)
                        throw new SocketException((int)SocketError.NotConnected);

                    // We are still connected but there's no data.
                    // Let other threads do their stuff while we wait.
                    Thread.Sleep(2);
                }
                else
                {
                    int readBytes = socket.Receive(buffer, totalReadBytes, byteCount - totalReadBytes, SocketFlags.None);
                    totalReadBytes += readBytes;
                }
            }
            if (totalReadBytes > byteCount) 
                AW2.Helpers.Log.Write("WARNING: Read " + totalReadBytes + " bytes when only " + byteCount + " was requested");
        }

        void ApplicationExitCallback(object caller, EventArgs args)
        {
            Dispose(false);
        }

        #endregion Private methods

        #region Private callback implementations

        /// <summary>
        /// Receives data from the remote host endlessly or until the socket
        /// is closed or there is some other error condition. 
        /// To be run in a separate thread.
        /// </summary>
        /// <seealso cref="System.Threading.ThreadStart"/>
        void ReadLoop()
        {
            try
            {
                while (true)
                {
                    // Read header.
                    Receive(headerReceiveBuffer, headerReceiveBuffer.Length);
                    if (!Message.IsValidHeader(headerReceiveBuffer))
                    {
                        string txt = "Connection received an invalid message header [length:" + 
                            headerReceiveBuffer.Length + " data:" +
                            string.Join(",", Array.ConvertAll<byte, string>(headerReceiveBuffer, 
                            a => ((byte)a).ToString("X2"))) + "]";
                        throw new InvalidDataException(txt);
                    }

                    // Read body.
                    int bodyLength = Message.GetBodyLength(headerReceiveBuffer);
                    if (bodyReceiveBuffer == null || bodyReceiveBuffer.Length < bodyLength)
                        bodyReceiveBuffer = new byte[bodyLength];
                    Receive(bodyReceiveBuffer, bodyLength);

                    // Add received message to the message queue.
                    Message message = Message.Deserialize(headerReceiveBuffer, bodyReceiveBuffer, Id);
                    messages.Enqueue(message);
                    lock (messages) if (MessageCallback != null) MessageCallback();
                }
            }
            catch (ThreadAbortException)
            {
                // Someone else terminated us, so he is handling the possible error condition.
            }
            catch (Exception e)
            {
                errors.Do(queue =>
                {
                    queue.Enqueue(e);
                    if (ErrorCallback != null) ErrorCallback();
                });
            }
        }

        /// <summary>
        /// Sends data to the remote host endlessly or until the socket
        /// is closed or there is some other error condition. 
        /// To be run in a separate thread.
        /// </summary>
        /// <seealso cref="System.Threading.ThreadStart"/>
        void SendLoop()
        {
            try
            {
                List<ArraySegment<byte>> sendSegments = new List<ArraySegment<byte>>();
                while (true)
                {
                    // Gather several messages together to reach a supposedly optimal
                    // TCP packet size of 1500 bytes minus some space for headers.
                    sendSegments.Clear();
                    int totalLength = 0;
                    sendBuffers.Do(queue =>
                    {
                        while (queue.Count > 0)
                        {
                            ArraySegment<byte> segment = queue.Peek();
                            if (sendSegments.Count > 0 && totalLength + segment.Count > 1400)
                                break;
                            totalLength += segment.Count;
                            sendSegments.Add(segment);
                            queue.Dequeue();
                        }
                    });
                    if (sendSegments.Count > 0)
                    {
                        int bytesSent = socket.Send(sendSegments);
                        if (bytesSent != totalLength)
                            throw new Exception("Not all data was sent (" + bytesSent + " out of " + totalLength + " bytes)");
                    }
                    else
                        Thread.Sleep(2);
                }
            }
            catch (ThreadAbortException)
            {
                // Someone else terminated us, so he is handling the possible error condition.
            }
            catch (Exception e)
            {
                errors.Do(queue =>
                {
                    queue.Enqueue(e);
                    if (ErrorCallback != null) ErrorCallback();
                });
            }
        }

        /// <summary>
        /// Callback implementation for accepting an incoming connection.
        /// </summary>
        /// <param name="asyncResult">The result of the asynchronous operation.</param>
        static void AcceptCallback(IAsyncResult asyncResult)
        {
            ConnectAsyncState state = (ConnectAsyncState)asyncResult.AsyncState;
            lock (connectionResults)
            {
                try
                {
                    Socket socketToNewHost = state.socket.EndAccept(asyncResult);
                    socketToNewHost.NoDelay = true;
                    var newConnection = new GameClientConnection(socketToNewHost);
                    connectionResults.Do(queue =>
                    {
                        queue.Enqueue(new Result<Connection>(newConnection, state.id));
                        if (ConnectionResultCallback != null) ConnectionResultCallback();
                    });

                    // Resume listening for connections.
                    if (serverSocket != null)
                        serverSocket.BeginAccept(AcceptCallback, state);
                }
                catch (Exception e)
                {
                    if (e is ObjectDisposedException && ((ObjectDisposedException)e).ObjectName == "System.Net.Sockets.Socket")
                    {
                        // This accept callback was triggered by the closing server socket.
                    }
                    else
                    {
                        connectionResults.Do(queue =>
                        {
                            queue.Enqueue(new Result<Connection>(e, state.id));
                            if (ConnectionResultCallback != null) ConnectionResultCallback();
                        });
                    }
                }
            }
        }

        /// <summary>
        /// Callback implementation for finishing an outgoing connection attempt.
        /// </summary>
        /// <param name="asyncResult">The result of the asynchronous operation.</param>
        static void ConnectCallback(IAsyncResult asyncResult)
        {
            ConnectAsyncState state = (ConnectAsyncState)asyncResult.AsyncState;
            lock (connectionResults)
            {
                try
                {
                    state.socket.EndConnect(asyncResult);
                    state.socket.NoDelay = true;
                    var newConnection = new GameServerConnection(state.socket);
                    connectionResults.Do(queue =>
                    {
                        queue.Enqueue(new Result<Connection>(newConnection, state.id));
                        if (ConnectionResultCallback != null) ConnectionResultCallback();
                    });
                }
                catch (Exception e)
                {
                    connectionResults.Do(queue =>
                    {
                        queue.Enqueue(new Result<Connection>(e, state.id));
                        if (ConnectionResultCallback != null) ConnectionResultCallback();
                    });
                }
            }
        }
        #endregion Private callback implementations
    }
}
