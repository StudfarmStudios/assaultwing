using System;
using System.Net.Sockets;
using AW2.Helpers;

namespace AW2.Net.ConnectionUtils
{
    public class AWTCPSocket : AWSocket
    {
        /// <param name="socket">An opened TCP socket to the remote host. This <see cref="AWSocket"/>
        /// instance owns the socket and will dispose of it.</param>
        public AWTCPSocket(Socket socket, MessageReadThread.MessageHandler messageHandler)
            : base(socket, messageHandler)
        {
            if (!socket.Connected) throw new ArgumentException("TCP socket not connected", "socket");
        }

        public override void Dispose(bool error)
        {
            if (!error)
            {
                if (_socket.Connected) _socket.Shutdown(SocketShutdown.Both);
            }
            base.Dispose(error);
        }

        protected override SuspendableThread CreateReadThread()
        {
            return new TCPMessageReadThread(_socket, ThreadExceptionHandler, _messageHandler);
        }

        protected override SuspendableThread CreateWriteThread()
        {
            return new TCPMessageSendThread(_socket, _sendBuffers, ThreadExceptionHandler);
        }
    }
}
