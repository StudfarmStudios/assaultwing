using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using AW2.Helpers;
using AW2.Net.Connections;

namespace AW2.Net.ConnectionUtils
{
    /// <summary>
    /// Listens to incoming connection attempts. After a TCP connection is established,
    /// waits for an UDP message, then replies to that via UDP, and waits for final
    /// acknowledgement via TCP. The result of the whole process is added to
    /// <see cref="ConnectionResults"/>.
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
        /// Each connection attempt results in a new item in <see cref="ConnectionResults"/>.
        /// </summary>
        /// <param name="port">The port at which to listen for incoming connections.</param>
        public void StartListening(int port)
        {
            try
            {
                CheckThread();
                if (_serverSocket != null) throw new InvalidOperationException("Already listening to incoming connections");
                CreateServerSocket(port);
                if (StaticPortMapper.IsSupported)
                {
                    Log.Write("Mapping server connection port with UPnP");
                    StaticPortMapper.EnsurePortMapped(port, "TCP");
                    StaticPortMapper.EnsurePortMapped(port, "UDP");
                }
                else
                {
                    Log.Write("UPnP not supported, make sure the server port " + port
                        + " is forwarded to your computer in your local network"); 
                }
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
            if (_serverSocket == null) throw new InvalidOperationException("Already not listening for incoming connections");
            StopListeningImpl();
        }

        private void StopListeningImpl()
        {
            if (StaticPortMapper.IsSupported)
            {
                Log.Write("Removing previous UPnP port mapping");
                try
                {
                    int port = ((IPEndPoint)_serverSocket.LocalEndPoint).Port;
                    StaticPortMapper.RemovePortMapping(port, "TCP");
                    StaticPortMapper.RemovePortMapping(port, "UDP");
                }
                catch (Exception e)
                {
                    Log.Write("Error while removing previous UPnP port mapping: " + e);
                }
            }
            _serverSocket.Close();
            _serverSocket = null;
            _listenResult = null;
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
            var result = ConnectAsyncState.ConnectionAttemptCallback(asyncResult, () => CreateClientConnection(asyncResult));
            if (result != null)
            {
                if (!result.Successful)
                    ReportResult(result);
                else
                {
                    ReportResult(result); // TODO: don't report yet; open UDP first !!!
                    // 1. server and client each bind a UDP socket to some available port
                    // 2. server and client send their UDP port numbers to each other via TCP
                    // 3. server and client receive each other's UDP port number via TCP
                    //    - UDP end point stored to Connection.RemoteUDPEndPoint
                    // 4. client "connects" its UDP socket to server's UDP endpoint
                    // 5. client starts sending periodical dummy "pong" messages via UDP
                    //    - this should make client's NAT accept incoming UDP traffic from server
                    //    - dummy "pong" messages are replaced by proper "pong" messages when first ping arrives
                    // 6. server receives dummy "pong" via UDP from client
                    // 7. server starts sending periodical UDP "ping" messages
                    //    - this should keep client's NAT happily forwarding server's UDP traffic to client
                    // 8. server calls ReportResult(result);
                    // 9. client starts sending UDP "pong" replies to server's "pings"
                }
            }
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
