using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using AW2.Helpers;

namespace AW2.Net
{
    /// <summary>
    /// A thread that sends data to the remote host until the socket
    /// is closed or there is some other error condition. 
    /// </summary>
    public class MessageSendThread : SuspendableStepwiseThread
    {
        private Socket _socket;
        private ThreadSafeWrapper<Queue<ArraySegment<byte>>> _sendBuffers;

        public MessageSendThread(Socket socket, ThreadSafeWrapper<Queue<ArraySegment<byte>>> sendBuffers, Action<Exception> exceptionHandler)
            : base("Message Send Thread", exceptionHandler)
        {
            _socket = socket;
            _sendBuffers = sendBuffers;
            SetAction(new StepwiseAction(KeepSendingMessages()));
        }

        // Stepwise method. Enumerated objects are undefined.
        private IEnumerable<object> KeepSendingMessages()
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
                        ArraySegment<byte> segment = queue.Peek();
                        if (sendSegments.Count > 0 && totalLength + segment.Count > 1400)
                            break;
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
