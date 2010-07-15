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
    public class TCPMessageSendThread : MessageSendThread
    {
        public TCPMessageSendThread(Socket socket, ThreadSafeWrapper<Queue<ArraySegment<byte>>> sendBuffers, Action<Exception> exceptionHandler)
            : base("TCP Message Send Thread", socket, sendBuffers, exceptionHandler)
        {
        }

        // Stepwise method. Enumerated objects are undefined.
        protected override IEnumerable<object> KeepSendingMessages()
        {
            var sendSegments = new List<ArraySegment<byte>>();
            while (true)
            {
                sendSegments.Clear();
                int totalLength = 0;
                _sendBuffers.Do(queue =>
                {
                    while (queue.Count > 0)
                    {
                        var segment = queue.Peek();
                        totalLength += segment.Count;
                        sendSegments.Add(segment);
                        queue.Dequeue();
                    }
                });
                if (sendSegments.Count > 0)
                {
                    int bytesSent = _socket.Send(sendSegments);
                    if (bytesSent != totalLength)
                        throw new NetworkException("Not all data was sent (" + bytesSent + " out of " + totalLength + " bytes)");
                }
                else
                    Thread.Sleep(0);
                yield return null;
            }
        }
    }
}
