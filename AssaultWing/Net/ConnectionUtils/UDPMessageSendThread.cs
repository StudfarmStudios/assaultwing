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
        public UDPMessageSendThread(Socket socket, ThreadSafeWrapper<Queue<NetBuffer>> sendBuffers, Action<Exception> exceptionHandler)
            : base("UDP Message Send Thread", socket, sendBuffers, exceptionHandler)
        {
            if (socket.ProtocolType != ProtocolType.Udp) throw new ArgumentException("Not a UDP socket", "socket");
        }

        // Stepwise method. Enumerated objects are undefined.
        protected override IEnumerable<object> KeepSendingMessages()
        {
            while (true)
            {
                byte[] buffer = null;
                _sendBuffers.Do(queue =>
                {
                    if (queue.Count > 0) buffer = queue.Dequeue().Buffer;
                });
                if (buffer != null)
                {
                    int bytesSent = _socket.Send(buffer);
                    if (bytesSent != buffer.Length)
                        throw new NetworkException("Not all data was sent (" + bytesSent + " out of " + buffer.Length + " bytes)");
                }
                else
                    Thread.Sleep(0);
                yield return null;
            }
        }
    }
}
