using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using AW2.Helpers;

namespace AW2.Net.ConnectionUtils
{
    public class AWTCPSocket : AWSocket
    {
        /// <param name="socket">An opened TCP socket to the remote host. This <see cref="AWSocket"/>
        /// instance owns the socket and will dispose of it.</param>
        /// <param name="messageHandler">Delegate that handles received message data.
        /// If null then no data will be received. The delegate is called in a background thread.</param>
        public AWTCPSocket(Socket socket, MessageHandler messageHandler)
            : base(socket, messageHandler)
        {
            if (!socket.Connected) throw new ArgumentException("TCP socket not connected", "socket");
        }

        protected override void StartReceiving()
        {
            var receiveArgs = new SocketAsyncEventArgs();
            receiveArgs.Completed += ReceiveCompleted;
            receiveArgs.SetBuffer(new byte[BUFFER_LENGTH], 0, Message.HEADER_LENGTH);
            ThreadPool.QueueUserWorkItem(state =>
            {
                UseSocket(socket =>
                {
                    var isPending = socket.ReceiveAsync(receiveArgs);
                    if (!isPending) ReceiveCompleted(socket, receiveArgs);
                });
            });
        }

        private void ReceiveCompleted(object sender, SocketAsyncEventArgs args)
        {
            // This method is called in a background thread. Catch and custom report all exceptions.
            try
            {
                bool isPending = false;
                do
                {
                    CheckSocketError(args);
                    if (args.SocketError != SocketError.Success) break;
                    if (args.BytesTransferred == 0)
                    {
                        Errors.Do(queue => queue.Enqueue("Connection closed"));
                        break;
                    }
                    var bytesTotal = args.Offset + args.BytesTransferred;
                    var bytesHandled = 0;
                    while (true)
                    {
                        var bufferSegment = new ArraySegment<byte>(args.Buffer, bytesHandled, bytesTotal - bytesHandled);
                        var moreBytesHandled = _messageHandler(bufferSegment, (IPEndPoint)args.RemoteEndPoint);
                        if (moreBytesHandled == 0) break;
                        bytesHandled += moreBytesHandled;
                    }
                    var bytesLeftOver = bytesTotal - bytesHandled;
                    if (bytesHandled > 0) Array.Copy(args.Buffer, bytesHandled, args.Buffer, 0, bytesLeftOver);
                    args.SetBuffer(bytesLeftOver, BUFFER_LENGTH - bytesLeftOver);
                    UseSocket(socket => isPending = socket.ReceiveAsync(args));
                }
                while (!isPending && !IsDisposed);
            }
            catch (Exception e)
            {
                var message = string.Format("{0} in ReceiveCompleted: {1}", e.GetType().ToString(),
                    e is SocketException ? ((SocketException)e).SocketErrorCode.ToString() : e.ToString());
                Errors.Do(queue => queue.Enqueue(message));
                Dispose();
            }
        }
    }
}
