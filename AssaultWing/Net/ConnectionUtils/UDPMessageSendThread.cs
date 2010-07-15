using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;

namespace AW2.Net.ConnectionUtils
{
    /// <summary>
    /// A thread that sends data to the remote host until the socket
    /// is closed or there is some other error condition. 
    /// </summary>
    public class UDPMessageSendThread : MessageSendThread
    {
        public UDPMessageSendThread(Socket socket, ThreadSafeWrapper<Queue<ArraySegment<byte>>> sendBuffers, Action<Exception> exceptionHandler)
            : base("UDP Message Send Thread", socket, sendBuffers, exceptionHandler)
        {
            if (socket.ProtocolType != ProtocolType.Udp) throw new ArgumentException("Not a UDP socket", "socket");
        }

        // Stepwise method. Enumerated objects are undefined.
        protected override IEnumerable<object> KeepSendingMessages()
        {
            while (true)
            {
                var segment = new ArraySegment<byte>();
                _sendBuffers.Do(queue =>
                {
                    if (queue.Count > 0) segment = queue.Dequeue();
                });
                if (segment.Array != null)
                {
                    int bytesSent = _socket.Send(segment.Array, segment.Offset, segment.Count, SocketFlags.None);
                    if (bytesSent != segment.Count)
                        throw new NetworkException("Not all data was sent (" + bytesSent + " out of " + segment.Count + " bytes)");
                }
                else
                    Thread.Sleep(0);
                yield return null;
            }
        }
    }
}
