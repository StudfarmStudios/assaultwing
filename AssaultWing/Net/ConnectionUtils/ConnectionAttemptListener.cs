﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
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

        private ConnectionAttemptListener() { }

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

        private void ListenOneConnection()
        {
            if (_serverSocket == null) throw new ApplicationException("Server socket must be opened first");
            try
            {
                _listenResult = _serverSocket.BeginAccept(AcceptCallback, new ConnectAsyncState(new Socket[] { _serverSocket }, null));
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
            return new GameClientConnection(socketToNewHost);
        }
    }
}
