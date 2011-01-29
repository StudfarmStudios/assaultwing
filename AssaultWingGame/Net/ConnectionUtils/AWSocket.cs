using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;
using AW2.Helpers;

namespace AW2.Net.ConnectionUtils
{
    /// <summary>
    /// Assault Wing wrapper around a Berkeley socket. Handles threads that read and write
    /// to the socket and stores received data.
    /// </summary>
    public abstract class AWSocket
    {
        public delegate void MessageHandler(ArraySegment<byte> messageHeaderAndBody, IPEndPoint remoteEndPoint);

        #region Fields

        protected const int BUFFER_LENGTH = 65536;
        private static readonly TimeSpan SEND_TIMEOUT = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan RECEIVE_TIMEOUT = TimeSpan.FromSeconds(10);
        private static readonly IPEndPoint UNSPECIFIED_IP_ENDPOINT = new IPEndPoint(IPAddress.Any, 0);

        private static Stack<SocketAsyncEventArgs> g_sendArgs = new Stack<SocketAsyncEventArgs>();

        private Socket _socket;
        protected MessageHandler _messageHandler;
        private int _isDisposed;

        #endregion Fields

        #region Properties

        /// <summary>
        /// Is the connection disposed and thus no longer usable.
        /// </summary>
        public bool IsDisposed { get { return _isDisposed > 0; } }

        /// <summary>
        /// The local end point of the socket in this host's local network.
        /// </summary>
        /// <see cref="System.Net.Sockets.Socket.LocalEndPoint"/>
        public IPEndPoint PrivateLocalEndPoint
        {
            get
            {
                var addresses = Dns.GetHostAddresses(Dns.GetHostName());
                var localIPAddress = addresses.First(address => address.AddressFamily == AddressFamily.InterNetwork); // IPv4 address
                return new IPEndPoint(localIPAddress, ((IPEndPoint)_socket.LocalEndPoint).Port);
            }
        }

        /// <summary>
        /// The remote end point of the socket.
        /// </summary>
        /// <see cref="System.Net.Sockets.Socket.RemoteEndPoint"/>
        public IPEndPoint RemoteEndPoint
        {
            get
            {
                if (IsDisposed) throw new InvalidOperationException("This socket has been disposed");
                return (IPEndPoint)_socket.RemoteEndPoint;
            }
        }

        /// <summary>
        /// Error messages produced by the socket.
        /// </summary>
        public ThreadSafeWrapper<Queue<string>> Errors { get; private set; }

        #endregion Properties

        /// <param name="socket">A socket to the remote host. This <see cref="AWSocket"/>
        /// instance owns the socket and will dispose of it.</param>
        /// <param name="messageHandler">Delegate that handles received message data.</param>
        protected AWSocket(Socket socket, MessageHandler messageHandler)
        {
            if (socket == null) throw new ArgumentNullException("socket", "Null socket argument");
            if (messageHandler == null) throw new ArgumentNullException("messageHandler", "Null message handler");
            Application.ApplicationExit += ApplicationExitCallback;
            _socket = socket;
            ConfigureSocket(_socket);
            _messageHandler = messageHandler;
            Errors = new ThreadSafeWrapper<Queue<string>>(new Queue<string>());
            StartReceiving();
        }

        public void Send(byte[] data)
        {
            Send(data, UNSPECIFIED_IP_ENDPOINT);
        }

        /// <summary>
        /// Sends raw byte data to the remote host. The data is sent asynchronously,
        /// so there is no guarantee when the transmission will be finished.
        /// </summary>
        public void Send(byte[] data, IPEndPoint remoteEndPoint)
        {
            // TODO: Let caller write data directly into sendArgs.Buffer !!! Expose a MemoryWriter and ask final data length from it
            var sendArgs = GetSendArgs(remoteEndPoint);
            sendArgs.SetBuffer(0, data.Length);
            Array.Copy(data, sendArgs.Buffer, data.Length);
            UseSocket(socket =>
            {
                var isPending = socket.SendToAsync(sendArgs);
                if (!isPending) SendToCompleted(socket, sendArgs);
            });
        }

        public void Dispose()
        {
            UseSocket(socket =>
            {
                if (socket.Connected) socket.Shutdown(SocketShutdown.Both);
            });
            if (Interlocked.Exchange(ref _isDisposed, 1) > 0) return;
            Application.ApplicationExit -= ApplicationExitCallback;
            UseSocket(socket => socket.Close());
        }

        protected abstract void StartReceiving();

        protected void CheckSocketError(SocketAsyncEventArgs args)
        {
            if (args.SocketError == SocketError.Success) return;
            Errors.Do(queue => queue.Enqueue(string.Format("Error in {0}: {1}", args.LastOperation, args.SocketError)));
        }

        protected void UseSocket(Action<Socket> action)
        {
            lock (_socket)
            {
                if (!IsDisposed) action(_socket);
            }
        }

        private static void ConfigureSocket(Socket socket)
        {
            socket.SendTimeout = (int)SEND_TIMEOUT.TotalMilliseconds;
            socket.ReceiveTimeout = (int)RECEIVE_TIMEOUT.TotalMilliseconds;
            DisableNagleAlgorithm(socket);
        }

        private static void DisableNagleAlgorithm(Socket socket)
        {
            if (socket.ProtocolType != ProtocolType.Tcp) return;
            try
            {
                socket.NoDelay = true;
            }
            catch (SocketException)
            {
                Log.Write("NOTE: Couldn't disable Nagle algorithm for TCP socket");
            }
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
                    (prop.Name == "MulticastLoopback" && socket.ProtocolType == ProtocolType.Tcp) ||
                    (prop.Name == "NoDelay" && socket.SocketType != SocketType.Stream) ||
                    (prop.Name == "LingerState" && socket.ProtocolType == ProtocolType.Udp) ||
                    (prop.Name == "RemoteEndPoint" && socket.ProtocolType == ProtocolType.Udp))
                    return null;
                return prop.Name + ": " + prop.GetValue(socket, null).ToString();
            }
            catch (System.Reflection.TargetInvocationException e)
            {
                Log.Write("Error reading Socket property " + prop.Name + ": " + e);
            };
            return null;
        }

        private SocketAsyncEventArgs GetSendArgs(IPEndPoint remoteEndPoint)
        {
            SocketAsyncEventArgs sendArgs = null;
            lock (g_sendArgs)
            {
                if (g_sendArgs.Any()) sendArgs = g_sendArgs.Pop();
            }
            if (sendArgs == null)
            {
                sendArgs = new SocketAsyncEventArgs();
                sendArgs.Completed += SendToCompleted;
                sendArgs.SetBuffer(new byte[BUFFER_LENGTH], 0, BUFFER_LENGTH);
            }
            // Note: SocketAsyncEventArgs.RemoteEndPoint is ignored by TCP sockets
            sendArgs.RemoteEndPoint = remoteEndPoint;
            return sendArgs;
        }

        private void SendToCompleted(object sender, SocketAsyncEventArgs args)
        {
            CheckSocketError(args);
            lock (g_sendArgs)
            {
                g_sendArgs.Push(args);
            }
        }

        private void ApplicationExitCallback(object caller, EventArgs args)
        {
            Dispose();
        }
    }
}
