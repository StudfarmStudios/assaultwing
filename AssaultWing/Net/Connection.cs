using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Net;
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
        /// State information for an asynchronous data send attempt.
        /// </summary>
        class SendAsyncState
        {
            public Socket socket;
            public byte[] data;
            public int startIndex;
            public SendAsyncState(Socket socket, byte[] data)
            {
                this.socket = socket;
                this.data = data;
                startIndex = 0;
            }
        }

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
        /// The thread that is continuously reading incoming data from the remote host.
        /// </summary>
        Thread readThread;

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

        #endregion Fields

        #region Properties

        /// <summary>
        /// Unique identifier of the connection. Nonnegative.
        /// </summary>
        public int Id { get; private set; }

        /// <summary>
        /// The local end point of the connection.
        /// </summary>
        /// <see cref="System.Net.Sockets.Socket.LocalEndPoint"/>
        public IPEndPoint LocalEndPoint { get { return (IPEndPoint)socket.LocalEndPoint; } }

        /// <summary>
        /// The remote end point of the connection.
        /// </summary>
        /// <see cref="System.Net.Sockets.Socket.RemoteEndPoint"/>
        public IPEndPoint RemoteEndPoint { get { return (IPEndPoint)socket.RemoteEndPoint; } }

        /// <summary>
        /// Received messages that are waiting for consumption by the client program.
        /// </summary>
        public TypedQueue<Message> Messages { get { return messages; } }

        /// <summary>
        /// Called after a new element has been added to <c>Messages</c>.
        /// </summary>
        public event Action MessageCallback;

        /// <summary>
        /// Results of connection attempts.
        /// </summary>
        public static ThreadSafeWrapper<Queue<Result<Connection>>> ConnectionResults { get { return connectionResults; } }

        /// <summary>
        /// Called after a new element has been added to <c>ConnectionResults</c>.
        /// </summary>
        public static event Action ConnectionResultCallback;

        /// <summary>
        /// Information on general error situations.
        /// </summary>
        public ThreadSafeWrapper<Queue<Exception>> Errors { get { return errors; } }

        /// <summary>
        /// Called after a new element has been added to <c>Errors</c>.
        /// </summary>
        public event Action ErrorCallback;

        #endregion Properties

        #region Kind-of constructors and destructor

        /// <summary>
        /// Are we listening for connection attempts from remote hosts.
        /// </summary>
        public static bool IsListening { get { lock (connectionResults) return serverSocket != null; } }

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
                serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Any, port);
                serverSocket.Bind(serverEndPoint);
                serverSocket.Listen(64);
                serverSocket.BeginAccept(AcceptCallback, new ConnectAsyncState(serverSocket, id));
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
        /// Creates a new connection to a remote host.
        /// </summary>
        /// <param name="socket">An opened socket to the remote host. The
        /// created connection owns the socket and will dispose of it.</param>
        /// The client program can create <c>Connection</c>s via the static methods
        /// <c>Listen()</c> and <c>Connect()</c>.
        Connection(Socket socket)
        {
            if (socket == null)
                throw new ArgumentNullException("Null socket argument");
            if (!socket.Connected)
                throw new ArgumentException("Socket not connected");
            Id = leastUnusedId++;
            Application.ApplicationExit += new EventHandler(delegate(object sender, EventArgs args) { this.Dispose(); });
            socket.Blocking = true;
            socket.ReceiveTimeout = 0; // don't time out
            this.socket = socket;
            headerReceiveBuffer = new byte[Message.HeaderLength];
            readThread = new Thread(ReadLoop);
            readThread.Start();
            messages = new TypedQueue<Message>();
            errors = new ThreadSafeWrapper<Queue<Exception>>(new Queue<Exception>());
        }

        /// <summary>
        /// Closes the connection and frees resources it has allocated.
        /// </summary>
        public void Dispose()
        {
            //socket.Shutdown();
            socket.Close();
            if (readThread != null && readThread.IsAlive)
            {
                readThread.Abort();
                readThread.Join();
            }
        }

        #endregion Kind-of constructors and destructor

        #region Send methods

        /// <summary>
        /// Sends a message to the remote host. The message is sent asynchronously,
        /// so there is no guarantee when the transmission will be finished.
        /// </summary>
        /// <param name="message">The message to send.</param>
        public void Send(Message message)
        {
            byte[] data = message.Serialize();
            Send(data);
        }

        /// <summary>
        /// Sends raw byte data to the remote host. The data is sent asynchronously,
        /// so there is no guarantee when the transmission will be finished.
        /// </summary>
        /// <param name="data">The data to send.</param>
        void Send(byte[] data)
        {
            socket.BeginSend(data, 0, data.Length, SocketFlags.None, SendCallback,
                new SendAsyncState(socket, data));
        }

        #endregion Send methods

        #region Private callback implementations

        /// <summary>
        /// Receives data from the remote host endlessly or until the socket
        /// is closed or there is some other error condition. 
        /// To be run in a separate thread.
        /// </summary>
        /// <see cref="System.Threading.ThreadStart"/>
        void ReadLoop()
        {
            try
            {
                while (true)
                {
                    // Read header.
                    int readBytes = socket.Receive(headerReceiveBuffer);
                    if (readBytes != headerReceiveBuffer.Length)
                        throw new Exception("Fatal program logic error: Socket.Receive got only " + readBytes + " bytes instead of " + headerReceiveBuffer.Length);
                    if (!Message.IsValidHeader(headerReceiveBuffer))
                        throw new InvalidDataException("Connection received an invalid message header");

                    // Read body.
                    int bodyLength = Message.GetBodyLength(headerReceiveBuffer);
                    if (bodyReceiveBuffer == null || bodyReceiveBuffer.Length < bodyLength)
                        bodyReceiveBuffer = new byte[bodyLength];
                    readBytes = socket.Receive(bodyReceiveBuffer, bodyLength, SocketFlags.None);
                    if (readBytes != bodyLength)
                        throw new Exception("Fatal program logic error: Socket.Receive got only " + readBytes + " bytes instead of " + bodyLength);

                    // Add received message to the message queue.
                    Message message = Message.Deserialize(headerReceiveBuffer, bodyReceiveBuffer, Id);
                    messages.Enqueue(message);
                    lock (messages) if (MessageCallback != null) MessageCallback();
                }
            }
            catch (Exception e)
            {
                errors.Do(delegate(Queue<Exception> queue) { queue.Enqueue(e); });
                lock (errors) if (ErrorCallback != null) ErrorCallback();
            }
            finally
            {
                Dispose();
            }
        }

        /// <summary>
        /// Callback implementation for finishing a data send to the remote host.
        /// </summary>
        /// <param name="asyncResult">The result of the asynchronous operation.</param>
        void SendCallback(IAsyncResult asyncResult)
        {
            SendAsyncState state = (SendAsyncState)asyncResult.AsyncState;
            try
            {
                int bytesSent = state.socket.EndSend(asyncResult);
                state.startIndex += bytesSent;
                if (state.startIndex < state.data.Length)
                {
                    // Not all data was sent this time; continue sending the remaining data.
                    state.socket.BeginSend(state.data, state.startIndex, state.data.Length - state.startIndex, 
                        SocketFlags.None, SendCallback, state);
                }
            }
            catch (Exception e)
            {
                errors.Do(delegate(Queue<Exception> queue) { queue.Enqueue(e); });
                lock (errors) if (ErrorCallback != null) ErrorCallback();
                Dispose();
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
                    Connection newConnection = new Connection(socketToNewHost);
                    connectionResults.Do(delegate(Queue<Result<Connection>> queue)
                    {
                        queue.Enqueue(new Result<Connection>(newConnection, state.id));
                    });
                    lock (connectionResults) if (ConnectionResultCallback != null) ConnectionResultCallback();

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
                        connectionResults.Do(delegate(Queue<Result<Connection>> queue)
                        {
                            queue.Enqueue(new Result<Connection>(e, state.id));
                        });
                        lock (connectionResults) if (ConnectionResultCallback != null) ConnectionResultCallback();
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
                    Connection newConnection = new Connection(state.socket);
                    connectionResults.Do(delegate(Queue<Result<Connection>> queue)
                    {
                        queue.Enqueue(new Result<Connection>(newConnection, state.id));
                    });
                    lock (connectionResults) if (ConnectionResultCallback != null) ConnectionResultCallback();
                }
                catch (Exception e)
                {
                    connectionResults.Do(delegate(Queue<Result<Connection>> queue)
                    {
                        queue.Enqueue(new Result<Connection>(e, state.id));
                    });
                    lock (connectionResults) if (ConnectionResultCallback != null) ConnectionResultCallback();
                }
            }
        }
        #endregion Private callback implementations
    }
}
