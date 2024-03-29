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
        /// <param name="messageHandler">Delegate that handles received message data.
        /// If null then no data will be received. The delegate is called in a background thread.</param>
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
                    // However, ConnectionReset is known to happen when the game server closes a client
                    // connection in a controlled fashion due to an unexpected message header. How a
                    // connectionless protocol can reveal that the remote host closed its connection may
                    // be a total mystery, but the error code is real.
                    if (args.SocketError != SocketError.Success && args.SocketError != SocketError.ConnectionReset) break;
                    if (args.SocketError == SocketError.Success)
                    {
                        var bufferSegment = new ArraySegment<byte>(args.Buffer, 0, args.BytesTransferred);
                        var endPoint = (IPEndPoint)args.RemoteEndPoint;
                        _messageHandler(bufferSegment, endPoint);
                    }
                }
                catch (MessageException)
                {
                    // Usually this happens when receiving messages from the management server before
                    // management server address has been resolved.
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
