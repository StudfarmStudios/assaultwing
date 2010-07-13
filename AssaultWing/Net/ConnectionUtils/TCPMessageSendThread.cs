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
                // Gather several messages together to reach a supposedly optimal
                // TCP packet size of 1500 bytes minus some space for headers.
                sendSegments.Clear();
                int totalLength = 0;
                _sendBuffers.Do(queue =>
                {
                    while (queue.Count > 0)
                    {
                        var segment = queue.Peek();
                        if (sendSegments.Count > 0 && totalLength + segment.Count > 1400) break;
                        totalLength += segment.Count;
                        sendSegments.Add(segment);
                        queue.Dequeue();
                    }
                });
                if (sendSegments.Count > 0)
                {
                    int bytesSent = _socket.Send(sendSegments);
                    if (bytesSent != totalLength)
                        throw new Exception("Not all data was sent (" + bytesSent + " out of " + totalLength + " bytes)");
                }
                else
                    Thread.Sleep(0);
                yield return null;
            }
        }
    }
}
