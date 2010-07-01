using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace AW2.Net
{
    /// <summary>
    /// Listens to incoming connection attempts.
    /// </summary>
    public class ConnectionAttemptListener
    {
        private static ConnectionAttemptListener g_instance;

        /// <summary>
        /// Server socket for listening to incoming connection attempts.
        /// <c>null</c> if not in use.
        /// </summary>
        private Socket _serverSocket;

        private IAsyncResult _listenResult;

        /// <summary>
        /// The only instance (Singleton pattern).
        /// </summary>
        public static ConnectionAttemptListener Instance
        {
            get
            {
                if (g_instance == null) g_instance = new ConnectionAttemptListener();
                return g_instance;
            }
        }

        public bool IsListening { get { return _serverSocket != null; } }
        public ThreadSafeWrapper<Queue<Result<Connection>>> ConnectionResults { get; private set; }

        private ConnectionAttemptListener()
        {
            ConnectionResults = new ThreadSafeWrapper<Queue<Result<Connection>>>(new Queue<Result<Connection>>());
        }

        /// <summary>
        /// Starts listening connection attempts from remote hosts.
        /// To be called from the main thread only.
        /// </summary>
        /// <param name="port">The port at which to listen for incoming connections.</param>
        public void StartListening(int port)
        {
            try
            {
                CheckThread();
                if (_serverSocket != null) throw new InvalidOperationException("Already listening to incoming connections");
                CreateServerSocket(port);
                StaticPortMapper.EnsurePortMapped(NetworkEngine.TCP_CONNECTION_PORT, "TCP");
                ListenOneConnection();
            }
            catch (Exception)
            {
                StopListeningImpl();
                throw;
            }
        }

        public void Update()
        {
            if (_serverSocket != null && _listenResult.IsCompleted) ListenOneConnection();
        }

        /// <summary>
        /// Stops listening for connection attempts from remote hosts.
        /// To be called from the main thread only.
        /// </summary>
        public void StopListening()
        {
            CheckThread();
            if (_serverSocket == null) throw new Exception("Already not listening for incoming connections");
            StopListeningImpl();
        }

        private void StopListeningImpl()
        {
            _serverSocket.Close();
            _serverSocket = null;
            _listenResult = null;
            StaticPortMapper.RemovePortMapping(NetworkEngine.TCP_CONNECTION_PORT, "TCP");
        }

        private static void CheckThread()
        {
            if (Thread.CurrentThread.ManagedThreadId != AssaultWing.Instance.ManagedThreadID)
                throw new InvalidOperationException("Method called from outside the main thread");
        }

        private void CreateServerSocket(int port)
        {
            _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Any, port);
            _serverSocket.Bind(serverEndPoint);
            _serverSocket.Listen(64);
        }

        private void ListenOneConnection()
        {
            if (_serverSocket == null) throw new ApplicationException("Server socket must be opened first");
            try
            {
                _listenResult = _serverSocket.BeginAccept(AcceptCallback, new ConnectAsyncState(_serverSocket));
            }
            catch (SocketException e)
            {
                ReportResult(new Result<Connection>(e));
            }
        }

        private void AcceptCallback(IAsyncResult asyncResult)
        {
            ConnectAsyncState.ConnectionAttemptCallback(asyncResult, () => CreateClientConnection(asyncResult), ReportResult);
        }

        private static Connection CreateClientConnection(IAsyncResult asyncResult)
        {
            var state = (ConnectAsyncState)asyncResult.AsyncState;
            var socketToNewHost = state.Socket.EndAccept(asyncResult);
            return new GameClientConnection(socketToNewHost);
        }

        private void ReportResult(Result<Connection> result)
        {
            ConnectionResults.Do(queue => queue.Enqueue(result));
        }
    }
}
