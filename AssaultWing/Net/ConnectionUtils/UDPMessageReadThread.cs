using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using AW2.Helpers;

namespace AW2.Net.ConnectionUtils
{
    /// <summary>
    /// A thread that receives data from a remote host via UDP until the socket
    /// is closed or there is some other error condition.
    /// </summary>
    public class UDPMessageReadThread : MessageReadThread
    {
        public UDPMessageReadThread(Socket socket, Action<Exception> exceptionHandler, MessageHandler messageHandler)
            : base("UDP Message Read Thread", socket, exceptionHandler, messageHandler)
        {
            if (socket.ProtocolType != ProtocolType.Udp) throw new ArgumentException("Not a UDP socket", "socket");
        }

        protected override IEnumerable<object> ReceiveHeaderAndBody(byte[] headerAndBodyBuffer)
        {
            if (headerAndBodyBuffer == null) throw new ArgumentNullException("headerAndBodyBuffer", "Cannot receive to null buffer");
            while (true)
            {
                int availableBytes = _socket.Available;
                if (availableBytes == 0)
                {
                    // See if the socket is still connected. If Poll() shows that there
                    // is data to read but Available is still zero, the socket must have
                    // been closed at the remote host.
                    if (_socket.Poll(100, SelectMode.SelectRead))
                    {
                        if (_socket.Available == 0) throw new SocketException((int)SocketError.NotConnected);
                    }

                    // We are still connected but there's no data.
                    // Let other threads do their stuff while we wait.
                    Thread.Sleep(0);
                }
                else
                {
                    // There is data to read. Therefore we can call Receive knowing that it will not block eternally.
                    try
                    {
                        _socket.Receive(headerAndBodyBuffer, 0, availableBytes, SocketFlags.None);
                    }
                    catch (SocketException e)
                    {
                        if (e.ErrorCode == (int)SocketError.MessageSize)
                            throw new MessageException("Message was larger than the read buffer", e);
                        throw;
                    }
                }
                yield return null;
            }
        }
    }
}
