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
                    if (args.BytesTransferred < args.Count)
                        args.SetBuffer(args.Offset + args.BytesTransferred, args.Count - args.BytesTransferred);
                    else if (args.BytesTransferred > args.Count)
                    {
                        Dispose();
                        Errors.Do(queue => queue.Enqueue("Socket received too many bytes (" + args.BytesTransferred + "/" + args.Count + ")"));
                    }
                    else
                    {
                        if (args.Offset + args.BytesTransferred == Message.HEADER_LENGTH)
                            ProcessReceivedHeader(args);
                        else
                            ProcessReceivedBody(args);
                    }
                    UseSocket(socket => isPending = socket.ReceiveAsync(args));
                }
                while (!isPending && !IsDisposed);
            }
            catch (Exception e)
            {
                var message = string.Format("{0} in ReceiveCompleted: {1}", e.GetType().ToString(),
                    e is SocketException ? ((SocketException)e).SocketErrorCode.ToString() : e.Message);
                Errors.Do(queue => queue.Enqueue(message));
                Dispose();
            }
        }

        private void ProcessReceivedHeader(SocketAsyncEventArgs args)
        {
            var buffer = new ArraySegment<byte>(args.Buffer);
            if (!Message.IsValidHeader(buffer))
            {
                Dispose();
                Errors.Do(queue => queue.Enqueue("Connection received an invalid message header [" +
                    MiscHelper.BytesToString(new ArraySegment<byte>(args.Buffer, 0, Message.HEADER_LENGTH)) + "]"));
            }
            else
                args.SetBuffer(Message.HEADER_LENGTH, Message.GetBodyLength(buffer));
        }

        private void ProcessReceivedBody(SocketAsyncEventArgs args)
        {
            var bufferSegment = new ArraySegment<byte>(args.Buffer, 0, args.Offset + args.Count);
            var endPoint = (IPEndPoint)args.RemoteEndPoint;
            _messageHandler(bufferSegment, endPoint);
            args.SetBuffer(0, Message.HEADER_LENGTH);
        }
    }
}
