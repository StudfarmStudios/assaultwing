using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;
using AW2.Helpers;
using AW2.Helpers.Collections;
using AW2.Helpers.Serialization;

namespace AW2.Net.ConnectionUtils
{
    /// <summary>
    /// Assault Wing wrapper around a Berkeley socket. Handles threads that read and write
    /// to the socket and stores received data.
    /// </summary>
    public abstract class AWSocket
    {
        private class SendData
        {
            public byte[] Buffer;
            public NetworkBinaryWriter Writer;
            public int ByteCount;
            public EndPoint RemoteEndPoint;
        }

        /// <summary>
        /// Returns the number of bytes that were handled. The remaining bytes will be available at the next call.
        /// </summary>
        public delegate int MessageHandler(ArraySegment<byte> messageHeaderAndBody, IPEndPoint remoteEndPoint);

        protected const int BUFFER_LENGTH = 65536;
        private static readonly TimeSpan SEND_TIMEOUT = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan RECEIVE_TIMEOUT = TimeSpan.FromSeconds(10);
        private static readonly IPEndPoint UNSPECIFIED_IP_ENDPOINT = new IPEndPoint(IPAddress.Any, 0);

        private static ConcurrentQueue<SendData> g_sendDatas = new ConcurrentQueue<SendData>();

        protected MessageHandler _messageHandler;
        private Socket _socket;
        private Dictionary<IPEndPoint, SendData> _sendCache;
        private WorkQueue<SendData> _sendQueue;
        private int _isDisposed;
        private IPEndPoint _privateLocalEndPoint;
        private byte[] _macAddress;

        public bool IsDisposed { get { return _isDisposed > 0; } }

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

        /// <param name="socket">A socket to the remote host. This <see cref="AWSocket"/>
        /// instance owns the socket and will dispose of it.</param>
        /// <param name="messageHandler">Delegate that handles received message data.
        /// If null then no data will be received. The delegate is called in a background thread.</param>
        protected AWSocket(Socket socket, MessageHandler messageHandler)
        {
            if (socket == null) throw new ArgumentNullException("socket", "Null socket argument");
            Application.ApplicationExit += ApplicationExitCallback;
            _socket = socket;
            _sendCache = new Dictionary<IPEndPoint, SendData>();
            _sendQueue = new WorkQueue<SendData>(SendItem, DisposeSocket);
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
            foreach (var sendData in _sendCache.Values)
            {
                var bytesWritten = (int)sendData.Writer.GetBaseStream().Position;
                sendData.ByteCount = bytesWritten;
                _sendQueue.Enqueue(sendData);
            }
            _sendCache.Clear();
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _isDisposed, 1) > 0) return;
            _sendQueue.NoMoreWork();
        }

        private void DisposeSocket()
        {
            // Note: Don't use UseSocket() because IsDisposed is already true but the socket isn't disposed.
            lock (_socket)
            {
                Log.Write("Disposing {0} socket", _socket.ProtocolType);
                if (_socket.Connected)
                    try
                    {
                        // Shutdown may throw "System.Net.Sockets.SocketException (0x80004005): An existing connection was forcibly closed by the remote host"
                        _socket.Shutdown(SocketShutdown.Both);
                    }
                    catch (SocketException) { }
                Application.ApplicationExit -= ApplicationExitCallback;
                _socket.Close();
            }
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

        private SendData GetSendData(IPEndPoint remoteEndPoint)
        {
            SendData sendData = null;
            if (!g_sendDatas.TryDequeue(out sendData))
            {
                sendData = new SendData { Buffer = new byte[BUFFER_LENGTH] };
                sendData.Writer = NetworkBinaryWriter.Create(new MemoryStream(sendData.Buffer));
            }
            sendData.Writer.Seek(0, SeekOrigin.Begin);
            sendData.RemoteEndPoint = remoteEndPoint; // Note: SendData.RemoteEndPoint is ignored by TCP sockets
            return sendData;
        }

        private void AddToSendBuffer(Action<NetworkBinaryWriter> writeData, IPEndPoint remoteEndPoint)
        {
            SendData sendData = null;
            if (!_sendCache.TryGetValue(remoteEndPoint, out sendData))
            {
                sendData = GetSendData(remoteEndPoint);
                _sendCache[remoteEndPoint] = sendData;
            }
            writeData(sendData.Writer);
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

        /// <summary>
        /// Called from a worker thread in <see cref="WorkQueue<SendData>"/>.
        /// </summary>
        private void SendItem(SendData sendData)
        {
            try
            {
                UseSocket(socket =>
                {
                    var sentByteCount = socket.SendTo(sendData.Buffer, sendData.ByteCount, SocketFlags.None, sendData.RemoteEndPoint);
                    if (sentByteCount != sendData.ByteCount) throw new NetworkException("Only " + sentByteCount + " bytes of " + sendData.ByteCount + " were sent");
                });
            }
            catch (SocketException e)
            {
                if (e.SocketErrorCode == SocketError.ConnectionReset)
                    Errors.Do(queue => queue.Enqueue(string.Format("Connection reset during send")));
                else
                    Errors.Do(queue => queue.Enqueue(string.Format("Error during send: {0}", e.SocketErrorCode)));
            }
            g_sendDatas.Enqueue(sendData);
        }

        private void ApplicationExitCallback(object caller, EventArgs args)
        {
            Dispose();
        }
    }
}
