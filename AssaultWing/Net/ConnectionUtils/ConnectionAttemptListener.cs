using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using AW2.Core;
using AW2.Helpers;
using AW2.Net.Connections;
using AW2.Net.Messages;
using AW2.Net.MessageHandling;

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
        /// <summary>
        /// Server socket for listening to incoming connection attempts.
        /// <c>null</c> if not in use.
        /// </summary>
        private Socket _serverSocket;
        private int _mappedTcpPort, _mappedUdpPort;

        private IAsyncResult _listenResult;
        private AssaultWing _game;

        public bool IsListening { get { return _serverSocket != null; } }

        public ConnectionAttemptListener(AssaultWing game)
        {
            _game = game;
        }

        /// <summary>
        /// Starts listening connection attempts from remote hosts.
        /// To be called from the main thread only.
        /// Each connection attempt results in a new item in <see cref="ConnectionResults"/>.
        /// </summary>
        /// <param name="tcpPort">The port at which to listen for incoming connections.</param>
        /// <param name="udpPort">UDP port for traffic with established connections.</param>
        public void StartListening(int tcpPort, int udpPort)
        {
            try
            {
                CheckThread();
                if (_serverSocket != null) throw new InvalidOperationException("Already listening to incoming connections");
                CreateServerSocket(tcpPort);
                if (StaticPortMapper.IsSupported)
                {
                    Log.Write("Mapping server ports with UPnP");
                    StaticPortMapper.EnsurePortMapped(tcpPort, "TCP");
                    _mappedTcpPort = tcpPort;
                    StaticPortMapper.EnsurePortMapped(udpPort, "UDP");
                    _mappedUdpPort = udpPort;
                }
                else
                {
                    Log.Write("UPnP not supported, make sure that TCP port " + tcpPort
                        + " and UDP port " + udpPort + " are forwarded to your computer in your local network");
                }
                ListenOneConnection(_game);
            }
            catch (Exception)
            {
                StopListening();
                throw;
            }
        }

        public void Update()
        {
            if (_serverSocket != null && _listenResult.IsCompleted) ListenOneConnection(_game);
        }

        /// <summary>
        /// Stops listening for connection attempts from remote hosts.
        /// To be called from the main thread only.
        /// </summary>
        public void StopListening()
        {
            CheckThread();
            if (StaticPortMapper.IsSupported)
            {
                Log.Write("Removing previous UPnP port mappings");
                try
                {
                    if (_mappedTcpPort != 0)
                        StaticPortMapper.RemovePortMapping(_mappedTcpPort, "TCP");
                    if (_mappedUdpPort != 0)
                        StaticPortMapper.RemovePortMapping(_mappedUdpPort, "UDP");
                }
                catch (Exception e)
                {
                    Log.Write("Error while removing previous UPnP port mapping: " + e);
                }
                finally
                {
                    _mappedTcpPort = 0;
                    _mappedUdpPort = 0;
                }
            }
            if (_serverSocket != null) _serverSocket.Close();
            _serverSocket = null;
            _listenResult = null;
        }

        private static void CheckThread()
        {
            if (Thread.CurrentThread.ManagedThreadId != AssaultWingCore.Instance.ManagedThreadID)
                throw new InvalidOperationException("Method called from outside the main thread");
        }

        private void CreateServerSocket(int port)
        {
            _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Any, port);
            _serverSocket.Bind(serverEndPoint);
            _serverSocket.Listen(64);
        }

        private void ListenOneConnection(AssaultWing game)
        {
            if (_serverSocket == null) throw new ApplicationException("Server socket must be opened first");
            try
            {
                _listenResult = _serverSocket.BeginAccept(AcceptCallback, new ConnectAsyncState(game, new Socket[] { _serverSocket }, null));
            }
            catch (SocketException e)
            {
                Connection.HandleNewConnection(new Result<Connection>(e));
            }
        }

        private void AcceptCallback(IAsyncResult asyncResult)
        {
            var result = ConnectAsyncState.ConnectionAttemptCallback(asyncResult, () => CreateClientConnection(asyncResult));
            Connection.HandleNewConnection(result);
        }

        private static Connection CreateClientConnection(IAsyncResult asyncResult)
        {
            var state = (ConnectAsyncState)asyncResult.AsyncState;
            var socketToNewHost = state.Sockets.Single().EndAccept(asyncResult);
            try
            {
                return new GameClientConnection(state.Game, socketToNewHost);
            }
            catch (Exception)
            {
                Log.Write("Closing client connection socket due to error");
                socketToNewHost.Close();
                throw;
            }
        }
    }
}
