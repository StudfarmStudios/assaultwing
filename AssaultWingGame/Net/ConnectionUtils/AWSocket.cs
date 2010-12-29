using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Windows.Forms;
using AW2.Helpers;
using System.Threading;
using System.Net;

namespace AW2.Net.ConnectionUtils
{
    /// <summary>
    /// Assault Wing wrapper around a Berkely socket. Handles threads that read and write
    /// to the socket and stores received data.
    /// </summary>
    public abstract class AWSocket
    {
        #region Fields

        private static readonly TimeSpan SEND_TIMEOUT_MILLISECONDS = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan RECEIVE_TIMEOUT_MILLISECONDS = TimeSpan.FromSeconds(10);

        protected Socket _socket;
        protected MessageReadThread.MessageHandler _messageHandler;
        private int _isDisposed;

        /// <summary>
        /// Buffer of serialised messages waiting to be pushed to the socket.
        /// </summary>
        protected ThreadSafeWrapper<Queue<NetBuffer>> _sendBuffers;

        /// <summary>
        /// The thread that is continuously reading incoming data from the socket.
        /// </summary>
        private SuspendableThread _readThread;

        /// <summary>
        /// The thread that is continuously sending outgoing data to the socket.
        /// </summary>
        private SuspendableThread _sendThread;

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
        /// Information about general error situations in background threads.
        /// </summary>
        public ThreadSafeWrapper<Queue<Exception>> Errors { get; private set; }

        #endregion Properties

        /// <param name="socket">A socket to the remote host. This <see cref="AWSocket"/>
        /// instance owns the socket and will dispose of it.</param>
        /// <param name="messageHandler">Delegate that handles received message data.</param>
        protected AWSocket(Socket socket, MessageReadThread.MessageHandler messageHandler)
        {
            if (socket == null) throw new ArgumentNullException("socket", "Null socket argument");
            if (messageHandler == null) throw new ArgumentNullException("messageHandler", "Null message handler");
            Application.ApplicationExit += ApplicationExitCallback;
            _socket = socket;
            ConfigureSocket(_socket);
            _messageHandler = messageHandler;
            _sendBuffers = new ThreadSafeWrapper<Queue<NetBuffer>>(new Queue<NetBuffer>());
            Errors = new ThreadSafeWrapper<Queue<Exception>>(new Queue<Exception>());
            _readThread = CreateReadThread();
            _sendThread = CreateWriteThread();
        }

        /// <summary>
        /// Call this once after constructor to start message threads.
        /// </summary>
        public void StartThreads()
        {
            _readThread.Start();
            _sendThread.Start();
        }

        /// <summary>
        /// Sends raw byte data to the remote host. The data is sent asynchronously,
        /// so there is no guarantee when the transmission will be finished.
        /// </summary>
        public void Send(byte[] data, IPEndPoint remoteEndPoint)
        {
            _sendBuffers.Do(queue => queue.Enqueue(new NetBuffer(data, remoteEndPoint)));
        }

        /// <summary>
        /// Returns the number of bytes waiting to be pushed through the socket.
        /// </summary>
        public int GetSendQueueSize()
        {
            if (IsDisposed) return 0;
            int count = 0;
            _sendBuffers.Do(queue => count += queue.Sum(buffer => buffer.Buffer.Length));
            return count;
        }

        /// <param name="error">If <c>true</c> then an internal error has occurred.</param>
        public virtual void Dispose(bool error)
        {
            if (Interlocked.Exchange(ref _isDisposed, 1) > 0) return;
            Application.ApplicationExit -= ApplicationExitCallback;
            TerminateThread(_readThread);
            TerminateThread(_sendThread);
            _readThread = null;
            _sendThread = null;
            _socket.Close();
        }

        protected abstract SuspendableThread CreateReadThread();
        protected abstract SuspendableThread CreateWriteThread();

        private void TerminateThread(SuspendableThread thread)
        {
            if (thread == null) return;
            thread.Terminate();
            if (!thread.Join(TimeSpan.FromSeconds(1)))
                AW2.Helpers.Log.Write("WARNING: Unable to kill " + thread.Name);
        }

        private void ApplicationExitCallback(object caller, EventArgs args)
        {
            Dispose(false);
        }

        protected void ThreadExceptionHandler(Exception e)
        {
            Errors.Do(queue => queue.Enqueue(e));
        }

        private static void ConfigureSocket(Socket socket)
        {
            socket.Blocking = true;
            socket.SendTimeout = (int)SEND_TIMEOUT_MILLISECONDS.TotalMilliseconds;
            socket.ReceiveTimeout = (int)RECEIVE_TIMEOUT_MILLISECONDS.TotalMilliseconds;
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
    }
}
