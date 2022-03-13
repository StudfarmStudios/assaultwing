using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using AW2.Helpers;
using AW2.Helpers.Serialization;

namespace AW2.Net.ConnectionUtils
{
    /// <summary>
    /// Assault Wing wrapper around a Berkeley socket. Handles threads that read and write
    /// to the socket and stores received data.
    /// </summary>
    public abstract class AWSocket
    {
        /// <summary>
        /// Returns the number of bytes that were handled. The remaining bytes will be available at the next call.
        /// </summary>
        public delegate int MessageHandler(ArraySegment<byte> messageHeaderAndBody, IPEndPoint remoteEndPoint);

        protected const int BUFFER_LENGTH = 65536;
        private static readonly TimeSpan SEND_TIMEOUT = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan RECEIVE_TIMEOUT = TimeSpan.FromSeconds(10);
        private static readonly IPEndPoint UNSPECIFIED_IP_ENDPOINT = new IPEndPoint(IPAddress.Any, 0);

        private static ConcurrentStack<SocketAsyncEventArgs> g_sendArgs = new ConcurrentStack<SocketAsyncEventArgs>();

        #region For "LagLog" debug printing
        private static System.Diagnostics.Stopwatch g_stopWatch = new System.Diagnostics.Stopwatch();
        private static RunningSequenceTimeSpan g_sendTimesUDP = new RunningSequenceTimeSpan(TimeSpan.FromSeconds(1));
        private static RunningSequenceTimeSpan g_sendTimesTCP = new RunningSequenceTimeSpan(TimeSpan.FromSeconds(1));
        private struct ProtocolAndSendTime
        {
            public ProtocolType Protocol;
            public TimeSpan SendTime;
        }
        #endregion

        protected MessageHandler _messageHandler;
        private Socket _socket;
        private Dictionary<IPEndPoint, Tuple<SocketAsyncEventArgs, NetworkBinaryWriter>> _sendCache;
        private int _isDisposed;
        private IPEndPoint _privateLocalEndPoint;
        private byte[] _macAddress;

        public bool IsDisposed { get { return _isDisposed > 0; } }

        static AWSocket()
        {
            g_stopWatch.Start();
        }

        /// <summary>
        /// The local end point of the socket in this host's local network.
        /// </summary>
        /// <see cref="System.Net.Sockets.Socket.LocalEndPoint"/>
        public IPEndPoint PrivateLocalEndPoint
        {
            get
            {
                if (_privateLocalEndPoint == null)
                {
                    var addresses = Dns.GetHostAddresses(Dns.GetHostName());
                    var localIPAddress = addresses.First(address => address.AddressFamily == AddressFamily.InterNetwork); // IPv4 address
                    _privateLocalEndPoint = new IPEndPoint(localIPAddress, ((IPEndPoint)_socket.LocalEndPoint).Port);
                }
                return _privateLocalEndPoint;
            }
        }

        public byte[] MACAddress
        {
            get
            {
                if (_macAddress == null) _macAddress = GetMACAddress();
                return _macAddress;
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

        public ThreadSafeWrapper<Queue<string>> Errors { get; private set; }

        public static string GetDebugPrintLagStringOrNull()
        {
            var now = g_stopWatch.Elapsed;
            var sendTimesUDP = g_sendTimesUDP.Prune(now).Clone();
            var sendTimesTCP = g_sendTimesTCP.Prune(now).Clone();
            if (sendTimesUDP.Count == 0 && sendTimesTCP.Count == 0) return null;
            return string.Format("UDP: {0} sends took Min={1} Max={2} Avg={3}" +
                "\tTCP: {4} sends took Min={5} Max={6} Avg={7}",
                sendTimesUDP.Count, sendTimesUDP.Min.Ms(), sendTimesUDP.Max.Ms(), sendTimesUDP.Average.Ms(),
                sendTimesTCP.Count, sendTimesTCP.Min.Ms(), sendTimesTCP.Max.Ms(), sendTimesTCP.Average.Ms());
        }

        /// <param name="socket">A socket to the remote host. This <see cref="AWSocket"/>
        /// instance owns the socket and will dispose of it.</param>
        /// <param name="messageHandler">Delegate that handles received message data.
        /// If null then no data will be received. The delegate is called in a background thread.</param>
        protected AWSocket(Socket socket, MessageHandler messageHandler)
        {
            if (socket == null) throw new ArgumentNullException("socket", "Null socket argument");
            // TODO: Peter: create / find a different exit callback system
            // Application.ApplicationExit += ApplicationExitCallback;
            _socket = socket;
            _sendCache = new Dictionary<IPEndPoint, Tuple<SocketAsyncEventArgs, NetworkBinaryWriter>>();
            ConfigureSocket(_socket);
            _messageHandler = messageHandler;
            Errors = new ThreadSafeWrapper<Queue<string>>(new Queue<string>());
            if (messageHandler != null) StartReceiving();
        }

        /// <summary>
        /// Adds raw byte data to the buffer to send to the remote host. Call <see cref="FlushSendBuffer"/> later.
        /// Use this method for TCP sockets.
        /// </summary>
        public void AddToSendBuffer(Action<NetworkBinaryWriter> writeData)
        {
            AddToSendBuffer(writeData, UNSPECIFIED_IP_ENDPOINT);
        }

        /// <summary>
        /// Sends raw byte data to the remote host. The data is sent asynchronously, so there is
        /// no guarantee when the transmission will be finished. Use this method for UDP sockets.
        /// </summary>
        public void Send(Action<NetworkBinaryWriter> writeData, IPEndPoint remoteEndPoint)
        {
            AddToSendBuffer(writeData, remoteEndPoint);
            FlushSendBuffer();
        }

        /// <summary>
        /// Sends all data buffered by <see cref="AddToSendBuffer"/> to corresponding remote hosts.
        /// The data is sent asynchronously, so there is no guarantee when the transmission will be finished.
        /// </summary>
        public void FlushSendBuffer()
        {
            foreach (var sendArgsAndWriter in _sendCache.Values)
            {
                var bytesWritten = (int)sendArgsAndWriter.Item2.GetBaseStream().Position;
                var sendArgs = sendArgsAndWriter.Item1;
                sendArgs.SetBuffer(0, bytesWritten);
                sendArgs.UserToken = new ProtocolAndSendTime
                {
                    Protocol = this is AWUDPSocket ? ProtocolType.Udp : ProtocolType.Tcp,
                    SendTime = g_stopWatch.Elapsed,
                };
                UseSocket(socket =>
                {
                    var isPending = socket.SendToAsync(sendArgs);
                    if (!isPending) SendToCompleted(socket, sendArgs);
                });
            }
            _sendCache.Clear();
        }

        public void Dispose()
        {
            UseSocket(socket =>
            {
                Log.Write("Disposing {0} socket", socket.ProtocolType);
                if (socket.Connected)
                    try
                    {
                        // Shutdown may throw "System.Net.Sockets.SocketException (0x80004005): An existing connection was forcibly closed by the remote host"
                        socket.Shutdown(SocketShutdown.Both);
                    }
                    catch (SocketException) { }
            });
            if (Interlocked.Exchange(ref _isDisposed, 1) > 0) return;
            // TODO: Peter: create / find a different exit callback system
            // Application.ApplicationExit -= ApplicationExitCallback;
            UseSocket(socket => socket.Close());
        }

        protected abstract void StartReceiving();

        protected void CheckSocketError(SocketAsyncEventArgs args)
        {
            if (args.SocketError == SocketError.Success) return;
            if (args.SocketError == SocketError.ConnectionReset)
                Errors.Do(queue => queue.Enqueue(string.Format("Connection reset during {0}", args.LastOperation)));
            else
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
            if (!g_sendArgs.TryPop(out sendArgs))
            {
                sendArgs = new SocketAsyncEventArgs();
                sendArgs.Completed += SendToCompleted;
                sendArgs.SetBuffer(new byte[BUFFER_LENGTH], 0, BUFFER_LENGTH);
            }
            // Note: SocketAsyncEventArgs.RemoteEndPoint is ignored by TCP sockets
            sendArgs.RemoteEndPoint = remoteEndPoint;
            return sendArgs;
        }

        private void AddToSendBuffer(Action<NetworkBinaryWriter> writeData, IPEndPoint remoteEndPoint)
        {
            Tuple<SocketAsyncEventArgs, NetworkBinaryWriter> sendArgsAndWriter = null;
            if (!_sendCache.TryGetValue(remoteEndPoint, out sendArgsAndWriter))
            {
                var sendArgs = GetSendArgs(remoteEndPoint);
                var writer = NetworkBinaryWriter.Create(new MemoryStream(sendArgs.Buffer));
                sendArgsAndWriter = Tuple.Create(sendArgs, writer);
                _sendCache[remoteEndPoint] = sendArgsAndWriter;
            }
            writeData(sendArgsAndWriter.Item2);
        }

        private byte[] GetMACAddress()
        {
            var nicIP = PrivateLocalEndPoint.Address;
            var addresses =
                from nic in NetworkInterface.GetAllNetworkInterfaces()
                where nic.GetIPProperties().UnicastAddresses.Any(addr => addr.Address.Equals(nicIP))
                select nic.GetPhysicalAddress().GetAddressBytes();
            return addresses.First();
        }

        private void SendToCompleted(object sender, SocketAsyncEventArgs args)
        {
            SendToCompleted_DebugPrintLag(args);
            CheckSocketError(args);
            g_sendArgs.Push(args);
        }

        private static void SendToCompleted_DebugPrintLag(SocketAsyncEventArgs args)
        {
            if (!AW2.Core.AssaultWing.Instance.Settings.Net.LagLog) return;
            try
            {
                var protocolAndSendTime = (ProtocolAndSendTime)args.UserToken;
                lock (g_stopWatch) // Lock ensures that entries to g_sendTimesXXX are always in temporal order.
                {
                    var now = g_stopWatch.Elapsed;
                    var elapsedTime = now - protocolAndSendTime.SendTime;
                    if (protocolAndSendTime.Protocol == ProtocolType.Udp) g_sendTimesUDP.Add(elapsedTime, now);
                    if (protocolAndSendTime.Protocol == ProtocolType.Tcp) g_sendTimesTCP.Add(elapsedTime, now);
                }
            }
            catch (Exception e)
            {
                Log.Write("Error during debug print", e);
            }
        }

        private void ApplicationExitCallback(object caller, EventArgs args)
        {
            Dispose();
        }
    }
}
