using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using AW2.Helpers;

namespace AW2.Net.ConnectionUtils
{
    public class AWUDPSocket : AWSocket
    {
        /// <summary>
        /// Throws SocketException on error.
        /// </summary>
        public AWUDPSocket(int port, MessageHandler messageHandler)
            : base(CreateSocket(port), messageHandler)
        {
        }

        protected override void StartReceiving()
        {
            var receiveArgs = new SocketAsyncEventArgs { RemoteEndPoint = new IPEndPoint(IPAddress.Any, 0) };
            receiveArgs.Completed += ReceiveFromCompleted;
            receiveArgs.SetBuffer(new byte[BUFFER_LENGTH], 0, BUFFER_LENGTH);
            ThreadPool.QueueUserWorkItem(state =>
            {
                UseSocket(socket =>
                {
                    var isPending = socket.ReceiveFromAsync(receiveArgs);
                    if (!isPending) ReceiveFromCompleted(socket, receiveArgs);
                });
            });
        }

        private static Socket CreateSocket(int port)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            var localEndPoint = new IPEndPoint(IPAddress.Any, port);
            socket.Bind(localEndPoint);
            return socket;
        }

        private void ReceiveFromCompleted(object sender, SocketAsyncEventArgs args)
        {
            // This method is called in a background thread. Catch and custom report all exceptions.
            bool isPending = false;
            do
            {
                try
                {
                    CheckSocketError(args);
                    // Note: UDP reception will stop indefinitely if there is a SocketError.
                    // This should be okay, assuming SocketError signals that the UDP socket is unusable.
                    if (args.SocketError != SocketError.Success) break;
                    var bufferSegment = new ArraySegment<byte>(args.Buffer, 0, args.BytesTransferred);
                    var endPoint = (IPEndPoint)args.RemoteEndPoint;
                    _messageHandler(bufferSegment, endPoint);
                }
                catch (Exception e)
                {
                    var message = string.Format("{0} in ReceiveFromCompleted: {1}", e.GetType().ToString(),
                        e is SocketException ? ((SocketException)e).SocketErrorCode.ToString() : e.ToString());
                    Errors.Do(queue => queue.Enqueue(message));
                }
                try
                {
                    UseSocket(socket => isPending = socket.ReceiveFromAsync(args));
                }
                catch (Exception e)
                {
                    var message = string.Format("{0} in ReceiveFromCompleted: {1}", e.GetType().ToString(),
                        e is SocketException ? ((SocketException)e).SocketErrorCode.ToString() : e.ToString());
                    Errors.Do(queue => queue.Enqueue(message));
                    // Note: UDP reception will stop indefinitely because we don't call ReceiveFromAsync any more.
                    // This hasn't happened ever(?) but if it did, it would probably be due to a SocketException
                    // which would mean that something is dramatically wrong. So maybe it's okay this way.
                }
            }
            while (!isPending && !IsDisposed);
        }
    }
}
