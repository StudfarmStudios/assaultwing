using System;
using System.Net;
using System.Net.Sockets;
using AW2.Helpers;

namespace AW2.Net.ConnectionUtils
{
    public class AWUDPSocket : AWSocket
    {
        public AWUDPSocket(MessageReadThread.MessageHandler messageHandler)
            : base(CreateSocket(), messageHandler)
        {
        }

        private static Socket CreateSocket()
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            var localEndPoint = new IPEndPoint(IPAddress.Any, 0);
            socket.Bind(localEndPoint);
            return socket;
        }

        public void Dispose()
        {
            base.Dispose(false);
        }

        protected override SuspendableThread CreateReadThread()
        {
            return new UDPMessageReadThread(_socket, ThreadExceptionHandler, _messageHandler);
        }

        protected override SuspendableThread CreateWriteThread()
        {
            return new UDPMessageSendThread(_socket, _sendBuffers, ThreadExceptionHandler);
        }
    }
}
