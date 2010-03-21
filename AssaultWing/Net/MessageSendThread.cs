using System;
using System.Collections.Generic;
using System.Threading;
using AW2.Helpers;
using System.Net.Sockets;

namespace AW2.Net
{
    /// <summary>
    /// A thread that sends data to the remote host until the socket
    /// is closed or there is some other error condition. 
    /// </summary>
    class MessageSendThread : SuspendableThread
    {
        Action<Exception> _exceptionHandler;
        Socket _socket;
        ThreadSafeWrapper<Queue<ArraySegment<byte>>> _sendBuffers;
        StepwiseAction _keepSendingMessages;

        public MessageSendThread(Socket socket, ThreadSafeWrapper<Queue<ArraySegment<byte>>> sendBuffers, Action<Exception> exceptionHandler)
            : base("Message Send Thread")
        {
            _exceptionHandler = exceptionHandler;
            _socket = socket;
            _sendBuffers = sendBuffers;
            _keepSendingMessages = new StepwiseAction(KeepSendingMessages());
        }

        protected override void OnDoWork()
        {
            try
            {
                while (!HasTerminateRequest())
                {
                    bool awokenByTerminate = SuspendIfNeeded();
                    if (awokenByTerminate) return;
                    _keepSendingMessages.InvokeStep();
                }
            }
            catch (Exception e)
            {
                _exceptionHandler(e);
            }
        }

        // Stepwise method. Enumerated objects are undefined.
        private IEnumerable<object> KeepSendingMessages()
        {
            List<ArraySegment<byte>> sendSegments = new List<ArraySegment<byte>>();
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
