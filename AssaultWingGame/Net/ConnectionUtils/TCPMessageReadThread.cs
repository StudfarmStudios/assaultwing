using System;
using System.Collections.Generic;
using System.Linq;
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
    public class TCPMessageReadThread : MessageReadThread
    {
        public TCPMessageReadThread(Socket socket, Action<Exception> exceptionHandler, MessageHandler messageHandler)
            : base("TCP Message Read Thread", socket, exceptionHandler, messageHandler)
        {
            if (socket.ProtocolType != ProtocolType.Tcp) throw new ArgumentException("Not a TCP socket", "socket");
        }

        protected override IEnumerable<object> ReceiveHeaderAndBody(NetBuffer headerAndBodyBuffer)
        {
            foreach (var dummy in ReadHeader(headerAndBodyBuffer)) yield return null;
            foreach (var dummy in ReadBody(headerAndBodyBuffer)) yield return null;
        }

        // Stepwise method. Enumerated objects are undefined.
        private IEnumerable<object> ReadHeader(NetBuffer headerAndBodyBuffer)
        {
            foreach (var dummy in Receive(new ArraySegment<byte>(headerAndBodyBuffer.Buffer, 0, Message.HEADER_LENGTH))) yield return null;
            headerAndBodyBuffer.Length = Message.HEADER_LENGTH;
            if (!Message.IsValidHeader(headerAndBodyBuffer.Buffer))
            {
                string headerContents = string.Join(",", headerAndBodyBuffer.Buffer
                    .Take(Message.HEADER_LENGTH)
                    .Select(a => a.ToString("X2"))
                    .ToArray());
                string txt = "Connection received an invalid message header [" + headerContents + "]";
                throw new MessageException(txt);
            }
        }

        // Stepwise method. Enumerated objects are undefined.
        private IEnumerable<object> ReadBody(NetBuffer headerAndBodyBuffer)
        {
            int bodyLength = Message.GetBodyLength(headerAndBodyBuffer.Buffer);
            if (Message.HEADER_LENGTH + bodyLength > Message.MAXIMUM_LENGTH) throw new MessageException("Too long message body [" + bodyLength + " bytes]");
            foreach (var dummy in Receive(new ArraySegment<byte>(headerAndBodyBuffer.Buffer, Message.HEADER_LENGTH, bodyLength))) yield return null;
            headerAndBodyBuffer.Length += bodyLength;
        }

        private IEnumerable<object> Receive(ArraySegment<byte> segment)
        {
            if (segment.Array == null) throw new ArgumentNullException("buffer", "Cannot receive to null buffer");
            if (segment.Count < 0) throw new ArgumentException("Cannot receive negative number of bytes", "byteCount");
            int totalReadBytes = 0;
            while (totalReadBytes < segment.Count)
            {
                if (_socket.Available == 0)
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
                    // Now there is data to read so Receive() should return rather quickly.
                    int readOffset = segment.Offset + totalReadBytes;
                    int readCount = segment.Count - totalReadBytes;
                    int readBytes = _socket.Receive(segment.Array, readOffset, readCount, SocketFlags.None);
                    totalReadBytes += readBytes;
                }
                yield return null;
            }
            if (totalReadBytes > segment.Count)
                AW2.Helpers.Log.Write("WARNING: Read " + totalReadBytes + " bytes when only " + segment.Count + " was requested");
        }
    }
}
